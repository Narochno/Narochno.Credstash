using System;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.KeyManagementService;
using Amazon.Runtime;
using Narochno.Credstash;

namespace CredstashTester
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var creds = new StoredProfileAWSCredentials();
            var stash = new Credstash(new CredstashOptions(), new AmazonKeyManagementServiceClient(creds, RegionEndpoint.EUWest1),
                new AmazonDynamoDBClient(creds, RegionEndpoint.EUWest1));
            //var val = stash.GetSecret("redis:host", null, new Dictionary<string, string>()
            //{
            //    { "environment", "beta"}
            //}).Result;

            foreach (var entry in stash.List().Result)
            {
                Console.WriteLine($"{entry.Name} v{entry.Version}");
            }
        }
    }
}
