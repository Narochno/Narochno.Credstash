using Amazon;

namespace Narochno.Credstash
{
    public class CredstashOptions
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
        /// If the number of stored items are more than 10, you can decrypt them parally if this number if more than 1. Dop marks the number of maximum parallel requests to DynamoDB and KMS. You need to make sure your read capacity unit is in place.
        /// </summary>
        public int DegreeOfParallelism { get; set; } = 1;

        /// <summary>
        /// There can be exceptions during configuration loading. If this is set to true, exceptions are suppressed.
        /// </summary>
        public bool SuppressErrors { get; set; } = true;
    }
}