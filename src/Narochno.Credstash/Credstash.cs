using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Narochno.Credstash
{
    public class Credstash
    {
        private static byte[] INITIALIZATION_VECTOR = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };

        private readonly CredstashOptions options;
        private readonly IAmazonKeyManagementService amazonKeyManagementService;
        private readonly IAmazonDynamoDB amazonDynamoDb;
        

        public Credstash(CredstashOptions options, IAmazonKeyManagementService amazonKeyManagementService, IAmazonDynamoDB amazonDynamoDb)
        {
            this.options = options;
            this.amazonKeyManagementService = amazonKeyManagementService;
            this.amazonDynamoDb = amazonDynamoDb;
        }

        public async Task<string> GetSecret(string name, string version = null, Dictionary<string, string> encryptionContext = null)
        {
            CredstashItem item;
            if (version == null)
            {
                var response = await amazonDynamoDb.QueryAsync(new QueryRequest()
                {
                    TableName = options.Table,
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
                });
                item = CredstashItem.From(response.Items[0]);
            }
            else
            {
                var response = await amazonDynamoDb.GetItemAsync(new GetItemRequest()
                {
                    TableName = options.Table,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        { "name", new AttributeValue(name)},
                        { "version", new AttributeValue(version)},
                    }
                });
                item = CredstashItem.From(response.Item);
            }

            var decryptResponse = await amazonKeyManagementService.DecryptAsync(new DecryptRequest()
            {
                CiphertextBlob = new MemoryStream(Convert.FromBase64String(item.Key)),
                EncryptionContext = encryptionContext
            });
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
            cipher.Init(false, new ParametersWithIV(ParameterUtilities.CreateKeyParameter("AES", key), INITIALIZATION_VECTOR));
            byte[] plaintext = cipher.DoFinal(contents);
            return Encoding.UTF8.GetString(plaintext);
        }
        
        public async Task<List<CredstashEntry>> List()
        {
            var response = await amazonDynamoDb.ScanAsync(new ScanRequest()
            {
                TableName = options.Table,
                ProjectionExpression = "#N, version",
                ExpressionAttributeNames = new Dictionary<string, string>()
                {
                    {"#N", "name"}
                }
            });

            var entries = new List<CredstashEntry>();
            foreach (var item in response.Items)
            {
                entries.Add(new CredstashEntry(item["name"].S, item["version"].S));
            }
            return entries;
        }
    }
}