using System.Security.Cryptography;

namespace CompactOT
{
    public interface HashAlgorithmProvider
    {
        HashAlgorithm CreateHashAlgorithm();

        int SecurityLevel { get; }
    }
}
