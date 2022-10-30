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
        public HashAlgorithmProvider HashAlgorithmProvider { get; private set; }

        public int SecurityLevel => HashAlgorithmProvider.SecurityLevel;

        public CryptoContext(RandomNumberGenerator randomNumberGenerator, HashAlgorithmProvider hashAlgorithmProvider)
        {
            RandomNumberGenerator = randomNumberGenerator;
            HashAlgorithmProvider = hashAlgorithmProvider;
        }

        public static CryptoContext CreateDefault()
        {            
            return CreateWithSecurityLevel(128);
        }

        public static CryptoContext CreateWithSecurityLevel(int securityLevel)
        {
            // based on https://en.wikipedia.org/wiki/Hash_function_security_summary
            HashAlgorithmProvider hashAlgorithmProvider;
            if (securityLevel <= 128)
            {
                hashAlgorithmProvider = new SHA256Provider();
            }
            else if (securityLevel <= 256)
            {
                hashAlgorithmProvider = new SHA512Provider();
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
                hashAlgorithmProvider
            );
        }

        public void Dispose()
        {
            RandomNumberGenerator.Dispose();
        }

    }
}
