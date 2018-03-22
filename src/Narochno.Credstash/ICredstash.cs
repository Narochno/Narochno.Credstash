using System.Collections.Generic;
using System.Threading.Tasks;
using Narochno.Primitives;

namespace Narochno.Credstash
{
    public interface ICredstash
    {
        Task<Optional<string>> GetSecretAsync(string name, string version = null, Dictionary<string, string> encryptionContext = null, bool throwOnInvalidCipherTextException = true);
        Task<List<CredstashEntry>> ListAsync();
        Task<IDictionary<string, Optional<string>>> GetAllAsync(string version = null, Dictionary<string, string> encryptionContext = null, bool throwOnInvalidCipherTextException = true);
        Task PutAsync(string name, string secret, string kmsKey = "alias/credstash", bool autoVersion = true, string versionArg = null, Dictionary<string, string> encryptionContext = null, int? expireEpoch = null);
    }
}