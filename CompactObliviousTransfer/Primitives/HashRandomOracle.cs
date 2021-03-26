// SPDX-FileCopyrightText: 2018 Jonas Nagy-Kuhlen <jonas.nagy-kuhlen@rwth-aachen.de>
// SPDX-License-Identifier: MIT
// Adopted from CompactMPC: https://github.com/jnagykuhlen/CompactMPC

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace CompactOT
{
    public class HashRandomOracle : RandomOracle
    {
        private HashAlgorithm _hashAlgorithm;
        private object _hashAlgorithmLock;

        public HashRandomOracle(HashAlgorithm hashAlgorithm)
        {
            if (hashAlgorithm == null)
                throw new ArgumentNullException(nameof(hashAlgorithm));

            _hashAlgorithm = hashAlgorithm;
            _hashAlgorithmLock = new object();
        }

        public IEnumerator<byte> InvokeForEnumerator(byte[] query)
        {
            byte[] seed;
            lock (_hashAlgorithmLock)
            {
                seed = _hashAlgorithm.ComputeHash(query);
            }

            using (MemoryStream stream = new MemoryStream(seed.Length + 4))
            {
                stream.Write(seed, 0, seed.Length);

                int counter = 0;
                while (counter < Int32.MaxValue)
                {
                    stream.Position = seed.Length;
                    stream.Write(BitConverter.GetBytes(counter), 0, 4);
                    stream.Position = 0;

                    byte[] block;
                    lock (_hashAlgorithmLock)
                    {
                        block = _hashAlgorithm.ComputeHash(stream);
                    }

                    foreach (byte blockByte in block)
                        yield return blockByte;

                    counter++;
                }
            }

            throw new InvalidOperationException("Random oracle cannot provide more data since the counter has reached its maximum value.");
        }

        public override RandomByteSequence Invoke(byte[] query)
        {
            // note(lumip): as an alternative, extend Linq to IEnumerator ?
            return new RandomByteSequence(InvokeForEnumerator(query));
        }
    }
}
