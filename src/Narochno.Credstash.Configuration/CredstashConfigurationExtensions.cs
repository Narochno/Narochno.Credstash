using Amazon.DynamoDBv2;
using Amazon.KeyManagementService;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;

namespace Narochno.Credstash.Configuration
{
    public static class CredstashConfigurationExtensions
    {
        public static IConfigurationBuilder AddCredstash(this IConfigurationBuilder builder, AWSCredentials credentials, 
            CredstashConfigurationOptions options = null)
        {
            options = options ?? new CredstashConfigurationOptions();

            var credstash = new Credstash(new CredstashOptions()
                {
                    Region = options.Region,
                    Table = options.Table
                }, new AmazonKeyManagementServiceClient(credentials, options.Region),
                new AmazonDynamoDBClient(credentials, options.Region));

            builder.Add(new CredstashConfigurationSource(credstash, options.EncryptionContext));
            return builder;
        }
    }
}