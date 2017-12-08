using System;
using System.Collections.Generic;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.KeyManagementService;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Narochno.Credstash;
using Narochno.Credstash.Configuration;

namespace CredstashTester
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var stash = new Credstash(new CredstashOptions(), new AmazonKeyManagementServiceClient(RegionEndpoint.EUWest1),
                   new AmazonDynamoDBClient(RegionEndpoint.EUWest1));

            var noneExistingKey1 = stash.GetSecretAsync(Guid.NewGuid().ToString()).Result;
            var noneExistingKey2 = stash.GetSecretAsync(Guid.NewGuid().ToString(), "1").Result;
            
            //var creds = new StoredProfileAWSCredentials();
            //var stash = new Credstash(new CredstashOptions(), new AmazonKeyManagementServiceClient(creds, RegionEndpoint.EUWest1),
            //    new AmazonDynamoDBClient(creds, RegionEndpoint.EUWest1));
            //var val = stash.GetSecret("redis:host", null, new Dictionary<string, string>()
            //{
            //    { "environment", "beta"}
            //}).Result;
            //Console.WriteLine(val);

            //foreach (var entry in stash.List().Result)
            //{
            //    Console.WriteLine($"{entry.Name} v{entry.Version}");
            //}

            AWSCredentials creds = new StoredProfileAWSCredentials();
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddCredstash(creds, new CredstashConfigurationOptions()
            {
                EncryptionContext = new Dictionary<string, string>()
                {
                    {"environment", "beta"}
                }
            });

            var config = configBuilder.Build();
            var beta = config.GetSection("beta");

            Print(beta, string.Empty);
        }

        private static void Print(IConfiguration c, string prefix)
        {
            foreach (var section in c.GetChildren())
            {
                if (!string.IsNullOrEmpty(section.Value))
                {
                    Console.WriteLine(prefix + ":" + section.Key + " " + section.Value);
                }
                Print(section, prefix + ":" + section.Key);
            }
        }
    }
}
