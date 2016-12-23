using System;

namespace Narochno.Credstash
{
    public class CredstashException : Exception
    {
        public CredstashException(string message)
            : base(message)
        {
        }

        public CredstashException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}