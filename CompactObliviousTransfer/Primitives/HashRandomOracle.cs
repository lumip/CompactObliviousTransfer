// SPDX-FileCopyrightText: 2018 Jonas Nagy-Kuhlen <jonas.nagy-kuhlen@rwth-aachen.de>, 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: MIT
// Adopted from CompactMPC: https://github.com/jnagykuhlen/CompactMPC

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace CompactOT
{
    public class HashRandomOracle : RandomOracle
    {
        private HashAlgorithmProvider _hashAlgorithmProvider;

        public HashRandomOracle(HashAlgorithmProvider hashAlgorithmProvider)
        {
            _hashAlgorithmProvider = hashAlgorithmProvider;
        }

        public IEnumerator<byte> InvokeForEnumerator(byte[] query)
        {
            HashAlgorithm hashAlgorithm = _hashAlgorithmProvider.CreateHashAlgorithm();

            byte[] seed = hashAlgorithm.ComputeHash(query);

            using (MemoryStream stream = new MemoryStream(seed.Length + 4))
            {
                stream.Write(seed, 0, seed.Length);

                uint counter = 0;
                do
                {
                    stream.Position = seed.Length;
                    stream.Write(BitConverter.GetBytes(counter), 0, 4);
                    stream.Position = 0;

                    byte[] block = hashAlgorithm.ComputeHash(stream);

                    foreach (byte blockByte in block)
                        yield return blockByte;

                    counter++;
                }
                while (counter != 0); // stops when counter reaches zero again (overflow)
            }

            throw new InvalidOperationException("Random oracle cannot provide more data since the counter has reached its maximum value.");
        }

        public override RandomByteSequence Invoke(byte[] query)
        {
            return new RandomByteSequence(InvokeForEnumerator(query));
        }
    }
}
