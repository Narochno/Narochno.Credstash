using System;

namespace Narochno.Credstash
{
    public class CredstashException : Exception
    {
        public CredstashException(string message)
            : base(message)
        {
        }
    }
}