using System.Security.Cryptography;

namespace CompactOT
{
    public class SHA512Provider : HashAlgorithmProvider
    {
        public int SecurityLevel => 256;

        public HashAlgorithm CreateHashAlgorithm()
        {
            return SHA512.Create();
        }
    }
}
