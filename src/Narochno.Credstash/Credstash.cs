using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Narochno.Credstash.Internal;
using Narochno.Primitives;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Narochno.Credstash
{
    public class Credstash : ICredstash
    {
        private static readonly byte[] _initializationVector = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
        private const int PADDING_LENGTH = 19;

        private readonly IAmazonKeyManagementService _amazonKeyManagementService;
        private readonly IAmazonDynamoDB _amazonDynamoDb;


        public Credstash(CredstashOptions options, IAmazonKeyManagementService amazonKeyManagementService, IAmazonDynamoDB amazonDynamoDb)
        {
            Options = options;
            _amazonKeyManagementService = amazonKeyManagementService;
            _amazonDynamoDb = amazonDynamoDb;
        }

        public CredstashOptions Options { get; }

        public async Task<Optional<string>> GetSecretAsync(string name, string version = null, Dictionary<string, string> encryptionContext = null, bool throwOnInvalidCipherTextException = true)
        {
            CredstashItem item;
            if (version == null)
            {
                var response = await _amazonDynamoDb.QueryAsync(new QueryRequest()
                {
                    TableName = Options.Table,
                    Limit = 1,
                    ScanIndexForward = false,
                    ConsistentRead = true,
                    KeyConditions = new Dictionary<string, Condition>()
                    {
                        {
                            "name", new Condition()
                            {
                                ComparisonOperator = ComparisonOperator.EQ,
                                AttributeValueList = new List<AttributeValue>()
                                {
                                    new AttributeValue(name)
                                }
                            }
                        }
                    }
                }).ConfigureAwait(false);

                item = CredstashItem.From(response.Items.FirstOrDefault());
            }
            else
            {
                var response = await _amazonDynamoDb.GetItemAsync(new GetItemRequest()
                {
                    TableName = Options.Table,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        { "name", new AttributeValue(name)},
                        { "version", new AttributeValue(version)},
                    }
                }).ConfigureAwait(false);
                item = CredstashItem.From(response.Item);
            }

            if (item == null)
            {
                return null;
            }

            DecryptResponse decryptResponse;
            try
            {
                decryptResponse = await _amazonKeyManagementService.DecryptAsync(new DecryptRequest()
                {
                    CiphertextBlob = new MemoryStream(Convert.FromBase64String(item.Key)),
                    EncryptionContext = encryptionContext
                }).ConfigureAwait(false);
            }
            catch (InvalidCiphertextException e)
            {
                if (throwOnInvalidCipherTextException)
                {
                    throw new CredstashException("Could not decrypt hmac key with KMS. The credential may " +
                                                 "require that an encryption context be provided to decrypt " +
                                                 "it.", e);
                }
                return new Optional<string>();
            }
            catch (Exception e)
            {
                throw new CredstashException("Decryption error", e);
            }
            var bytes = decryptResponse.Plaintext.ToArray();
            var key = new byte[32];
            var hmacKey = new byte[32];
            Buffer.BlockCopy(bytes, 0, key, 0, 32);
            Buffer.BlockCopy(bytes, 32, hmacKey, 0, 32);

            var contents = Convert.FromBase64String(item.Contents);

            var hmac = new HMACSHA256(hmacKey);
            var result = hmac.ComputeHash(contents);

            if (!result.ToHexString().Equals(item.Hmac))
            {
                throw new CredstashException($"HMAC Failure for {item.Name} v{item.Version}");
            }

            IBufferedCipher cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
            cipher.Init(false, new ParametersWithIV(ParameterUtilities.CreateKeyParameter("AES", key), _initializationVector));
            byte[] plaintext = cipher.DoFinal(contents);
            return Encoding.UTF8.GetString(plaintext);
        }
        
        public async Task<List<CredstashEntry>> ListAsync()
        {
            var response = await _amazonDynamoDb.ScanAsync(new ScanRequest()
            {
                TableName = Options.Table,
                ProjectionExpression = "#N, version",
                ExpressionAttributeNames = new Dictionary<string, string>()
                {
                    {"#N", "name"}
                }
            }).ConfigureAwait(false);

            var entries = new List<CredstashEntry>();
            foreach (var item in response.Items)
            {
                entries.Add(new CredstashEntry(item["name"].S, item["version"].S));
            }

            return entries;
        }

        public async Task<IDictionary<string, Optional<string>>> GetAllAsync(string version = null, Dictionary<string, string> encryptionContext = null, bool throwOnInvalidCipherTextException = true)
        {
            var secrets = await ListAsync().ConfigureAwait(false);
            var secretValueDict = new Dictionary<string, Optional<string>>();
            foreach (var secret in secrets)
            {
                var secretValue = await GetSecretAsync(secret.Name, version, encryptionContext, throwOnInvalidCipherTextException).ConfigureAwait(false);
                if (secretValue.HasValue)
                    secretValueDict[secret.Name] = secretValue;
            }
            return secretValueDict;
        }

        public async Task PutAsync(string name, string secret, string kmsKey = "alias/credstash", bool autoVersion = true, string versionArg = null, Dictionary<string, string> encryptionContext = null, int? expireEpoch = null)
        {
            var version = autoVersion ? IntWithPadding((await GetHighestVersion(name).ConfigureAwait(false) + 1).ToString()) : IntWithPadding(versionArg ?? string.Empty);
            var keyDataResponse = await GenerateKeyData(kmsKey, encryptionContext).ConfigureAwait(false);
            var encryptionResponse = SealAes(keyDataResponse, secret);

            var key = Convert.ToBase64String(keyDataResponse.CiphertextBlob);
            var contents = Convert.ToBase64String(encryptionResponse.CipherText);
            var hmac = encryptionResponse.Hmac.ToHexString();

            var credstashItem = new CredstashItem(name, version, contents, CredstashItem.DefaultDigest, hmac, key);

            await _amazonDynamoDb.PutItemAsync(new PutItemRequest
            {
                Item = CredstashItem.ToAttributeValueDict(credstashItem, expireEpoch),
                TableName = Options.Table,
                ConditionExpression = "attribute_not_exists(#name)",
                ExpressionAttributeNames = new Dictionary<string, string> { { "#name", "name" } }
            }).ConfigureAwait(false);
        }
        
        private async Task<KeyDataResponse> GenerateKeyData(string keyId, Dictionary<string, string> encryptionContext)
        {
            try
            {
                var kmsResponse = await _amazonKeyManagementService.GenerateDataKeyAsync(new GenerateDataKeyRequest
                {
                    EncryptionContext = encryptionContext,
                    KeyId = keyId,
                    NumberOfBytes = 64
                }).ConfigureAwait(false);

                return new KeyDataResponse
                {
                    CiphertextBlob = kmsResponse.CiphertextBlob.ToArray(),
                    Plaintext = kmsResponse.Plaintext.ToArray()
                };
            }
            catch (Exception e)
            {
                throw new CredstashException("Encryption Key Generation error", e);
            }
        }

        private static EncryptionResponse SealAes(KeyDataResponse keyDataResponse, string secret)
        {
            var plainText = Encoding.UTF8.GetBytes(secret);
            var bytes = keyDataResponse.Plaintext.ToArray();
            var dataKey = new byte[32];
            var hmacKey = new byte[32];
            Buffer.BlockCopy(bytes, 0, dataKey, 0, 32);
            Buffer.BlockCopy(bytes, 32, hmacKey, 0, 32);
            
            IBufferedCipher cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
            cipher.Init(true, new ParametersWithIV(ParameterUtilities.CreateKeyParameter("AES", dataKey), _initializationVector));
            var cipherText = cipher.DoFinal(plainText);

            var hmac = new HMACSHA256(hmacKey);
            var hmacResult = hmac.ComputeHash(cipherText);

            return new EncryptionResponse
            {
                CipherText = cipherText,
                Hmac = hmacResult
            };
        }

        private async Task<int> GetHighestVersion(string name)
        {
            var response = await _amazonDynamoDb.QueryAsync(new QueryRequest
            {
                TableName = Options.Table,
                Limit = 1,
                ScanIndexForward = false,
                ConsistentRead = true,
                KeyConditionExpression = "#name = :v_name",
                ExpressionAttributeNames = new Dictionary<string, string> { { "#name", "name" } },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> { { ":v_name", new AttributeValue(name) } },
                ProjectionExpression = "version"
            }).ConfigureAwait(false);
            if (!response.Items.Any())
            {
                return 0;
            }
            return response.Items.First().TryGetValue("version", out var versionValue)
                ? int.TryParse(versionValue.S, out var version) ? version : 0
                : 0;
        }

        private static string IntWithPadding(string version)
        {
            return version.PadLeft(PADDING_LENGTH, '0');
        }
    }
}