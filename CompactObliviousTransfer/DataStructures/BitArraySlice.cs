using System;
using System.Collections.Generic;
using System.Linq;
using CompactCryptoGroupAlgebra;

namespace CompactOT.DataStructures
{

    public class BitArraySlice : BitSequence
    {

        BitSequence _array;
        int _start;
        int _stopBefore;

        public BitArraySlice(BitSequence array, int start, int stopBefore)
        {
            if (array.Length < stopBefore)
                throw new ArgumentOutOfRangeException($"Bit array slice [{start}, {stopBefore}] exceeds bit array length {array.Length}.");
            if (stopBefore <= start)
                throw new ArgumentOutOfRangeException($"Start {start} must be before stop {stopBefore}.");
            if (start < 0)
                throw new ArgumentOutOfRangeException($"Start must not be negative; was {start}.");

            _array = array;
            _start = start;
            _stopBefore = stopBefore;
        }

        public override int Length => _stopBefore - _start;

        public override bool IsSynchronized => false;

        public override object? SyncRoot => _array.SyncRoot;

        public override bool IsReadOnly => true;

        private IEnumerable<byte> AsByteEnumerableInternalUnfiltered()
        {
            int byteOffset = _start / 8;
            int bitOffset = _start % 8;
            int lastByteOffset = (_stopBefore - 1) / 8;
            int numberOfBytes = (lastByteOffset + 1 - byteOffset);
            
            int numberOfOutputBytes = NumberLength.FromBitLength(Length).InBytes;
            var unfilteredByteEnumerable = new ShiftedByteArrayEnumerable(
                _array.AsByteEnumerable().Skip(byteOffset).Take(numberOfBytes),
                bitOffset
            ).Take(numberOfOutputBytes);

            return unfilteredByteEnumerable;
        }

        public override IEnumerable<byte> AsByteEnumerable()
        {
            var unfilteredByteEnumerable = AsByteEnumerableInternalUnfiltered();

            return new EnumeratedBitArrayView(unfilteredByteEnumerable, Length).AsByteEnumerable();
        }

        public override IEnumerator<Bit> GetEnumerator()
        {
            return new ByteToBitEnumerable(AsByteEnumerableInternalUnfiltered(), _stopBefore - _start).GetEnumerator();
        }
    }

}