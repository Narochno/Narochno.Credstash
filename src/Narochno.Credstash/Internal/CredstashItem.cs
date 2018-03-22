using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using System.IO;

namespace Narochno.Credstash.Internal
{
    public class CredstashItem
    {
        public const string DefaultDigest = "SHA256";

        public string Name { get; set; }
        public string Version { get; set; }
        public string Contents { get; set; }
        public string Digest { get; set; }
        public string Hmac { get; set; }
        public string Key { get; set; }

        public CredstashItem(string name, string version, string contents, string digest, string hmac, string key)
        {
            Name = name;
            Version = version;
            Contents = contents;
            Digest = digest;
            Hmac = hmac;
            Key = key;
        }

        public static CredstashItem From(Dictionary<string, AttributeValue> item)
        {
            if (item == null || item.Count == 0)
            {
                return null;
            }

            return new CredstashItem
            (
                item["name"].S,
                item["version"].S,
                item["contents"].S,
                (item.ContainsKey("digest") ? item["digest"]?.S : null) ?? DefaultDigest,
                GetHmacString(item["hmac"]),
                item["key"].S
            );
        }

        public static Dictionary<string, AttributeValue> ToAttributeValueDict(CredstashItem item, int? expireEpoch = null)
        {
            var attributeDict = new Dictionary<string, AttributeValue>
            {
                {"name", new AttributeValue(item.Name)},
                {"version", new AttributeValue(item.Version)},
                {"contents", new AttributeValue(item.Contents)},
                {"digest", new AttributeValue(item.Digest)},
                {"hmac", new AttributeValue(item.Hmac)},
                {"key", new AttributeValue(item.Key)}
            };
            if (expireEpoch.HasValue)
            {
                attributeDict["expires"] = new AttributeValue { N = expireEpoch.ToString() };
            }
            return attributeDict;
        }

        /// <summary>
        /// Attempts to return the string value of the hmac value, if it exists. Otherwise
        /// reads from the binary memory stream of the given value and returns the string
        /// value of the stream.
        /// </summary>
        /// <param name="hmacValue"></param>
        /// <returns></returns>
        private static string GetHmacString(AttributeValue hmacValue)
        {
            if (!string.IsNullOrWhiteSpace(hmacValue.S))
            {
                return hmacValue.S;
            }

            return hmacValue.B != null ? new StreamReader(hmacValue.B).ReadToEnd() : null;
        }
    }
}
