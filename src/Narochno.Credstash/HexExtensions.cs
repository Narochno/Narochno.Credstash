using System;

namespace Narochno.Credstash
{
    public static class HexExtensions
    {
        public static string ToHexString(this byte[] ba)
        {
            string hex = BitConverter.ToString(ba).ToLowerInvariant();
            return hex.Replace("-", "");
        }
    }
}