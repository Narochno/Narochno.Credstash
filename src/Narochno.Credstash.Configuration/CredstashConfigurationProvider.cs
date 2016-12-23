using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Narochno.Credstash.Configuration
{
    public class CredstashConfigurationProvider : ConfigurationProvider
    {
        private readonly Credstash credstash;
        private readonly Dictionary<string, string> encryptionContext;

        public CredstashConfigurationProvider(Credstash credstash, Dictionary<string, string> encryptionContext)
        {
            this.credstash = credstash;
            this.encryptionContext = encryptionContext;
        }

        public override void Load()
        {
            Data = LoadAsync().Result;
        }

        public async Task<Dictionary<string, string>> LoadAsync()
        {
            var data = new Dictionary<string, string>();
            foreach (var name in (await credstash.List()).Select(x => x.Name).Distinct())
            {
                try
                {
                    var value = await credstash.GetSecret(name, null, encryptionContext);
                    data.Add(name, value);
                }
                catch (Exception)
                {
                    //eat everything
                }
            }
            return data;
        }
    }
}
