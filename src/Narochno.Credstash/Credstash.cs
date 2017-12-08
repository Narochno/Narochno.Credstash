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
    }
}