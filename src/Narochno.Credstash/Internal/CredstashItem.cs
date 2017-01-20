using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;

namespace Narochno.Credstash.Internal
{
    public class CredstashItem
    {
        public const string DEFAULT_DIGEST = "SHA256";

        public string Name { get; set; }
        public string Version { get; set; }
        public string Contents { get; set; }
        public string Digest { get; set; }
        public string Hmac { get; set; }
        public string Key { get; set; }

        public static CredstashItem From(Dictionary<string, AttributeValue> item)
        {
            return new CredstashItem
            {
                Name = item["name"].S,
                Version = item["version"].S,
                Contents = item["contents"].S,
                Digest = (item.ContainsKey("digest") ? item["digest"]?.S : null) ?? DEFAULT_DIGEST,
                Hmac = item["hmac"].S,
                Key = item["key"].S,
            };
        }
    }
}