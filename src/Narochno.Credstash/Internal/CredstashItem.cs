using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;

namespace Narochno.Credstash
{
    public class CredstashItem
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Contents { get; set; }
        public string Digest { get; set; }
        public string Hmac { get; set; }
        public string Key { get; set; }

        public static CredstashItem From(Dictionary<string, AttributeValue> item)
        {
            return new CredstashItem()
            {
                Name = item["name"].S,
                Version = item["version"].S,
                Contents = item["contents"].S,
                Digest = item["digest"].S,
                Hmac = item["hmac"].S,
                Key = item["key"].S,
            };
        }
    }
}