namespace Narochno.Credstash
{
    public class CredstashEntry
    {
        public CredstashEntry(string name, string version)
        {
            Name = name;
            Version = version;
        }

        public string Name { get; }
        public string Version { get; }
    }
}