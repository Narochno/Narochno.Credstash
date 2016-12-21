using Amazon;

namespace Narochno.Credstash
{
    public class CredstashOptions
    {
        public string Table { get; set; } = "credential-store";
        public RegionEndpoint Region { get; set; } = RegionEndpoint.EUWest1;
    }
}