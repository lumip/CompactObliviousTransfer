using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using CompactCryptoGroupAlgebra;

namespace CompactOT.DataStructures
{
    public class EnumeratedBitArrayView : BitSequence
    {
        private IEnumerable<byte> _byteFeed;
        private int _numberOfBits;
        public EnumeratedBitArrayView(IEnumerable<byte> asBytes, int numberOfBits)
        {
            _byteFeed = asBytes;
            _numberOfBits = numberOfBits;
        }

        public EnumeratedBitArrayView(IEnumerable<Bit> bits, int numberOfBits)
            : this(new BitToByteEnumerable(bits), numberOfBits)
        { }

        public EnumeratedBitArrayView(BitSequence bits)
            : this(bits.AsByteEnumerable(), ((ICollection<Bit>)bits).Count) { }

        public static EnumeratedBitArrayView FromBytes(byte[] buffer, int byteOffset, int numberOfBits)
        {
            return new EnumeratedBitArrayView(buffer.Skip(byteOffset), numberOfBits);
        }

        public override int Length => _numberOfBits;

        public override bool IsReadOnly => true;

        public override bool IsSynchronized => false;

        public override object SyncRoot => this;

        public override IEnumerable<byte> AsByteEnumerable()
        {
            var enumerator = _byteFeed.GetEnumerator();

            int numberOfBytes = NumberLength.FromBitLength(_numberOfBits).InBytes;
            for (int i = 0; i < numberOfBytes; ++i)
            {
                if (!enumerator.MoveNext()) // _byteFeed does not offer enough bytes to satisfy specified _numberOfBits
                    throw new BaseEnumeratorExhaustedException();

                byte current = enumerator.Current;

                // in the last byte, we mask away most significant bits if their position is larger than specified _numberOfBits
                int remainingBits = _numberOfBits - i * 8;
                if (remainingBits < 8)
                {
                    byte mask = (byte)((1 << remainingBits) - 1);
                    current = (byte)(current & mask);
                }
                yield return current;
            }
        }

        public override IEnumerator<Bit> GetEnumerator()
        {
            return new ByteToBitEnumerable(_byteFeed, _numberOfBits).GetEnumerator();
        }
        
    }
}