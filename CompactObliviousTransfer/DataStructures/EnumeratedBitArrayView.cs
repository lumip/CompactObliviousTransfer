using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CompactOT.DataStructures
{
    public class EnumeratedBitArrayView : BitArrayBase
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

        public EnumeratedBitArrayView(BitArrayBase bits)
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
            return _byteFeed.Take((_numberOfBits + 7) / 8);
        }

        public override IEnumerator<Bit> GetEnumerator()
        {
            return new ByteToBitEnumerable(AsByteEnumerable(), _numberOfBits).GetEnumerator();
        }
        
    }
}