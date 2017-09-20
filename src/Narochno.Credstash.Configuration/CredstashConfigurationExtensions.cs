using Amazon.DynamoDBv2;
using Amazon.KeyManagementService;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using System;

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
                Table = options.Table,
                DegreeOfParallelism = options.DegreeOfParallelism
            }, new AmazonKeyManagementServiceClient(credentials, options.Region),
                new AmazonDynamoDBClient(credentials, options.Region));

            builder.Add(new CredstashConfigurationSource(credstash, options.EncryptionContext));
            return builder;
        }

        public static IConfigurationBuilder AddCredstash(this IConfigurationBuilder builder, Action<CredstashConfigurationOptions> optionsAction, AWSCredentials creds = null)
        {
            var options = new CredstashConfigurationOptions();
            optionsAction(options);

            creds = creds ?? FallbackCredentialsFactory.GetCredentials();

            var credstash = new Credstash(new CredstashOptions()
            {
                Region = options.Region,
                Table = options.Table,
                DegreeOfParallelism = options.DegreeOfParallelism
            }, new AmazonKeyManagementServiceClient(creds, options.Region),
                new AmazonDynamoDBClient(creds, options.Region));

            builder.Add(new CredstashConfigurationSource(credstash, options.EncryptionContext));
            return builder;
        }
    }
}