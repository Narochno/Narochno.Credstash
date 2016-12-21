using Amazon;
using Amazon.DynamoDBv2;
using Amazon.KeyManagementService;
using Amazon.Runtime;

namespace Narochno.Credstash
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var creds = new BasicAWSCredentials("AKIAJUOYPFBETUDQILDA", "5ywC0S7unYRK5V+PDGKZW/2LeRuuWKgYn3fuNrbo");
            var stash = new Credstash(new CredstashOptions(), new AmazonKeyManagementServiceClient(creds, RegionEndpoint.EUWest1), 
                new AmazonDynamoDBClient(creds, RegionEndpoint.EUWest1));
            //var val = stash.GetSecret("redis:host", null, new Dictionary<string, string>()
            //{
            //    { "environment", "beta"}
            //}).Result;

            stash.List().Wait();
            //Console.WriteLine(val);
        }
    }
}
