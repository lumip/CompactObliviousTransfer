using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Globalization;


using CompactCryptoGroupAlgebra;
using CompactCryptoGroupAlgebra.Multiplicative;

namespace CompactOT
{

    public sealed class CryptoContext<TSecret, TCrypto> : IDisposable  where TSecret: notnull where TCrypto: notnull
    {
        public RandomNumberGenerator RandomNumberGenerator { get; private set; }
        public HashAlgorithm HashAlgorithm { get; private set; }
        public CryptoGroup<TSecret, TCrypto> CryptoGroup { get; private set; }

        public CryptoContext(RandomNumberGenerator randomNumberGenerator, HashAlgorithm hashAlgorithm, CryptoGroup<TSecret, TCrypto> cryptoGroup)
        {
            RandomNumberGenerator = randomNumberGenerator;
            HashAlgorithm = hashAlgorithm;
            CryptoGroup = cryptoGroup;
        }

        public static CryptoContext<BigInteger, BigInteger> CreateDefaultWith768BitGroup()
        {
            // Recommendation from RFC 2409, 768-bit MODP group, id 1
            string primeHex = @"0
                FFFFFFFF FFFFFFFF C90FDAA2 2168C234 C4C6628B 80DC1CD1
                29024E08 8A67CC74 020BBEA6 3B139B22 514A0879 8E3404DD
                EF9519B3 CD3A431B 302B0A6D F25F1437 4FE1356D 6D51C245
                E485B576 625E7EC6 F44C42E9 A63A3620 FFFFFFFF FFFFFFFF";
                
            var p = BigPrime.CreateWithoutChecks(BigInteger.Parse(Regex.Replace(primeHex, @"\s+", ""), NumberStyles.AllowHexSpecifier));
            var q = BigPrime.CreateWithoutChecks((p - 1) / 2);
            BigInteger g = 4;

            return new CryptoContext<BigInteger, BigInteger>(
                RandomNumberGenerator.Create(),
                SHA1.Create(),
                MultiplicativeGroupAlgebra.CreateCryptoGroup(p, q, g)
            );
        }

        public void Dispose()
        {
            RandomNumberGenerator.Dispose();
            HashAlgorithm.Dispose();
        }

    }
}
