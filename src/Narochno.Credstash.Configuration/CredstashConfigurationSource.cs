using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Narochno.Credstash.Configuration
{
    public class CredstashConfigurationSource : IConfigurationSource
    {
        private readonly Credstash credstash;
        private readonly Dictionary<string, string> encryptionContext;

        public CredstashConfigurationSource(Credstash credstash, Dictionary<string, string> encryptionContext)
        {
            this.credstash = credstash;
            this.encryptionContext = encryptionContext;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new CredstashConfigurationProvider(credstash, encryptionContext);    
        }
    }
}