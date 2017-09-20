using System.Collections.Generic;
using Amazon;

namespace Narochno.Credstash.Configuration
{
    public class CredstashConfigurationOptions
    {
        /// <summary>
        /// The name of your DynamoDB table where your secrets are stored in.
        /// </summary>
        public string Table { get; set; } = "credential-store";

        /// <summary>
        /// The region your DynamoDB table and KMS key are located at.
        /// </summary>
        public RegionEndpoint Region { get; set; } = RegionEndpoint.EUWest1;

        /// <summary>
        /// The encryption context key value pairs that were associated with the credential when it was put in DynamoDB.
        /// </summary>
        public Dictionary<string, string> EncryptionContext { get; set; }

        /// <summary>
        /// If the number of stored items are more than 10, you can decrypt them paralelly if this number if higher than 1. Dop marks the number of maximum parallel requests to DynamoDB and KMS. You need to make sure your read capacity unit is in place.
        /// </summary>
        public int Dop { get; set; }
    }
}