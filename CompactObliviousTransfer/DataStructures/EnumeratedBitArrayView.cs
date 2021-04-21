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
        public EnumeratedBitArrayView(IEnumerable<byte> operationOutputEnumerable, int numberOfBits)
        {
            _byteFeed = operationOutputEnumerable;
            _numberOfBits = numberOfBits;
        }

        public EnumeratedBitArrayView(IBitArray bits)
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
            return _byteFeed;
        }

        public override IEnumerator<Bit> GetEnumerator()
        {
            return new BitEnumerable(AsByteEnumerable(), _numberOfBits).GetEnumerator();
        }
        
    }
}