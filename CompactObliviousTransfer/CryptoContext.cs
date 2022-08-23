// SPDX-FileCopyrightText: 2018 Jonas Nagy-Kuhlen <jonas.nagy-kuhlen@rwth-aachen.de>, 2022 Lukas Prediger <lumip@lumip.de>
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

        public int SecurityLevel => HashAlgorithm.HashSize / 2;

        public CryptoContext(RandomNumberGenerator randomNumberGenerator, HashAlgorithm hashAlgorithm)
        {
            RandomNumberGenerator = randomNumberGenerator;
            HashAlgorithm = hashAlgorithm;
        }

        public static CryptoContext CreateDefault()
        {            
            return CreateWithSecurityLevel(128);
        }

        public static CryptoContext CreateWithSecurityLevel(int securityLevel)
        {
            // based on https://en.wikipedia.org/wiki/Hash_function_security_summary
            HashAlgorithm hashAlgorithm;
            if (securityLevel <= 128)
            {
                hashAlgorithm = SHA256.Create();
            }
            else if (securityLevel <= 256)
            {
                hashAlgorithm = SHA512.Create();
            }
            else
            {
                throw new ArgumentOutOfRangeException(
                    $"Cannot create crypto context. No hash function satisfies the required security level of {securityLevel}",
                    nameof(securityLevel)
                );
            }
            return new CryptoContext(
                RandomNumberGenerator.Create(),
                hashAlgorithm
            );
        }

        public void Dispose()
        {
            RandomNumberGenerator.Dispose();
            HashAlgorithm.Dispose();
        }

    }
}
