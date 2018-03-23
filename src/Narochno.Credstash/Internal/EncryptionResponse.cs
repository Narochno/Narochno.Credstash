namespace Narochno.Credstash.Internal
{
    internal class EncryptionResponse
    {
        public byte[] CipherText { get; set; }
        public byte[] Hmac { get; set; }
    }
}
