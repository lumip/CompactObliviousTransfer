// SPDX-FileCopyrightText: 2018 Jonas Nagy-Kuhlen <jonas.nagy-kuhlen@rwth-aachen.de>
// SPDX-License-Identifier: MIT
// Adopted from CompactMPC: https://github.com/jnagykuhlen/CompactMPC

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompactOT.DataStructures;

namespace CompactOT
{

    public abstract class RandomOracle
    {
        public abstract RandomByteSequence Invoke(byte[] query);

        public virtual RandomByteSequence Invoke(IEnumerable<byte> query)
        {
            return Invoke(query.ToArray());
        }
        
        public byte[] Mask(byte[] message, byte[] query)
        {
            return Mask(((IEnumerable<byte>)message), query).ToArray();
        }

        public IEnumerable<byte> Mask(IEnumerable<byte> message, IEnumerable<byte> query)
        {
            var messageEnumerator = message.GetEnumerator();
            var maskEnumerator = Invoke(query).Enumerator;

            while (messageEnumerator.MoveNext())
            {
                if (!maskEnumerator.MoveNext())
                    throw new ArgumentException("Random oracle invocation does not provide enough data to mask the given message.", nameof(query));

                yield return (byte)(messageEnumerator.Current ^ maskEnumerator.Current);
            }
        }

        public BitSequence Mask(BitSequence message, BitSequence query)
        {
            return Mask(message, query.AsByteEnumerable());
        }

        public BitSequence Mask(BitSequence message, IEnumerable<byte> query)
        {
            return new EnumeratedBitArrayView(
                Mask(message.AsByteEnumerable(), query),
                message.Length
            );
        }
    }
}
