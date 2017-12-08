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

        public static CredstashItem From(Dictionary<string, AttributeValue> item)
        {
            if (item == null || item.Count == 0)
            {
                return null;
            }

            return new CredstashItem
            {
                Name = item["name"].S,
                Version = item["version"].S,
                Contents = item["contents"].S,
                Digest = (item.ContainsKey("digest") ? item["digest"]?.S : null) ?? DefaultDigest,
                Hmac = GetHmacString(item["hmac"]),
                Key = item["key"].S,
            };
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
