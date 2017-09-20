using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Narochno.Credstash.Configuration
{
    public class CredstashConfigurationSource : IConfigurationSource
    {
        private readonly Credstash _credstash;
        private readonly Dictionary<string, string> _encryptionContext;

        public CredstashConfigurationSource(Credstash credstash, Dictionary<string, string> encryptionContext)
        {
            _credstash = credstash;
            _encryptionContext = encryptionContext;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new CredstashConfigurationProvider(_credstash, _encryptionContext);    
        }
    }
}