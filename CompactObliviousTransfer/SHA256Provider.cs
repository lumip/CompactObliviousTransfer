using System.Security.Cryptography;

namespace CompactOT
{
    public class SHA256Provider : HashAlgorithmProvider
    {
        public int SecurityLevel => 128;

        public HashAlgorithm CreateHashAlgorithm()
        {
            return SHA256.Create();
        }
    }
}
