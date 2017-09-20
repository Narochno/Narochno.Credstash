using Amazon;
using Amazon.DynamoDBv2;
using Amazon.KeyManagementService;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

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
                Dop = options.Dop
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
                Dop = options.Dop
            }, new AmazonKeyManagementServiceClient(creds, options.Region),
                new AmazonDynamoDBClient(creds, options.Region));

            builder.Add(new CredstashConfigurationSource(credstash, options.EncryptionContext));
            return builder;
        }

        public static IConfigurationBuilder AddCredstash(this IConfigurationBuilder builder, RegionEndpoint region,
            string table,
            AWSCredentials creds = null,
            Dictionary<string, string> encryptionContext = null,
            int dop = 1)
        {
            var options = new CredstashConfigurationOptions
            {
                Region = region,
                Dop = dop,
                EncryptionContext = encryptionContext ?? new Dictionary<string, string>(),
                Table = table
            };

            creds = creds ?? FallbackCredentialsFactory.GetCredentials();

            var credstash = new Credstash(new CredstashOptions()
            {
                Region = options.Region,
                Table = options.Table,
                Dop = options.Dop
            },
                new AmazonKeyManagementServiceClient(creds, region),
                new AmazonDynamoDBClient(creds, region)
            );

            builder.Add(new CredstashConfigurationSource(credstash, options.EncryptionContext));

            return builder;
        }
    }
}