// SPDX-FileCopyrightText: 2018 Jonas Nagy-Kuhlen <jonas.nagy-kuhlen@rwth-aachen.de>
// SPDX-License-Identifier: MIT
// Adopted from CompactMPC: https://github.com/jnagykuhlen/CompactMPC

using System;
using System.Security.Cryptography;

namespace CompactOT
{

    public sealed class CryptoContext : IDisposable
    {
        public RandomNumberGenerator RandomNumberGenerator { get; private set; }
        public HashAlgorithm HashAlgorithm { get; private set; }

        public CryptoContext(RandomNumberGenerator randomNumberGenerator, HashAlgorithm hashAlgorithm)
        {
            RandomNumberGenerator = randomNumberGenerator;
            HashAlgorithm = hashAlgorithm;
        }

        public static CryptoContext CreateDefault()
        {            
            return new CryptoContext(
                RandomNumberGenerator.Create(),
                SHA1.Create()
            );
        }

        public void Dispose()
        {
            RandomNumberGenerator.Dispose();
            HashAlgorithm.Dispose();
        }

    }
}
