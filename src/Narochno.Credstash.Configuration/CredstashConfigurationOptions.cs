using System.Collections.Generic;
using Amazon;

namespace Narochno.Credstash.Configuration
{
    public class CredstashConfigurationOptions
    {
        public string Table { get; set; } = "credential-store";
        public RegionEndpoint Region { get; set; } = RegionEndpoint.EUWest1;

        public Dictionary<string, string> EncryptionContext { get; set; }
    }
}