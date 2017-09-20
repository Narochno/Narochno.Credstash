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
        private readonly int dop;
        private readonly Dictionary<string, string> encryptionContext;

        public CredstashConfigurationProvider(Credstash credstash, Dictionary<string, string> encryptionContext)
        {
            this.credstash = credstash;
            this.dop = credstash.Options.Dop;
            this.encryptionContext = encryptionContext;
        }

        public override void Load()
        {
            Data = LoadAsync().Result;
        }

        public async Task<Dictionary<string, string>> LoadAsync()
        {
            var data = new Dictionary<string, string>();

            var entries = (await credstash.ListAsync()).Select(x => x.Name).Distinct();

            if (entries.Count() > 10 && dop > 1)
            {
                await entries.ForEachAsync(dop, async entry =>
                {
                    await SetConfigValueAsync(data, entry).ConfigureAwait(false);

                }).ConfigureAwait(false);
            }
            else
            {
                foreach (var entry in entries)
                {
                    await SetConfigValueAsync(data, entry).ConfigureAwait(false);
                }
            }

            return data;
        }

        private async Task SetConfigValueAsync(Dictionary<string, string> data, string entry)
        {
            try
            {
                var secret = await credstash.GetSecretAsync(entry, null, encryptionContext, false).ConfigureAwait(false);

                if (secret.HasValue)
                {
                    data.Add(entry, secret.Value);
                }
            }
            catch (Exception)
            {
                //eat everything
            }
        }
    }
}
