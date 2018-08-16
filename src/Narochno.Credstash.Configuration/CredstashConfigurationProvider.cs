using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Narochno.Credstash.Configuration
{
    public class CredstashConfigurationProvider : ConfigurationProvider
    {
        private readonly Credstash _credstash;
        private readonly int _degreeOfParallelism;
        private readonly Dictionary<string, string> _encryptionContext;
        private readonly bool _suppressErrors;

        public CredstashConfigurationProvider(Credstash credstash, Dictionary<string, string> encryptionContext)
        {
            _credstash = credstash;
            _degreeOfParallelism = credstash.Options.DegreeOfParallelism;
            _encryptionContext = encryptionContext;
            _suppressErrors = credstash.Options.SuppressErrors;
        }

        public override void Load()
        {
            Data = LoadAsync().Result;
        }

        public async Task<IDictionary<string, string>> LoadAsync()
        {
            var data = new ConcurrentDictionary<string, string>();

            var entries = (await _credstash.ListAsync().ConfigureAwait(false)).Select(x => x.Name).Distinct().ToList();

            if (entries.Count > 10 && _degreeOfParallelism > 1)
            {
                await entries.ForEachAsync(_degreeOfParallelism, async entry =>
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

        private async Task SetConfigValueAsync(ConcurrentDictionary<string, string> data, string entry)
        {
            try
            {
                var secret = await _credstash.GetSecretAsync(entry, null, _encryptionContext, false).ConfigureAwait(false);

                if (secret.HasValue)
                {
                    data.TryAdd(entry, secret.Value);
                }
            }
            catch
            {
                if (!_suppressErrors)
                    throw;
            }
        }
    }
}
