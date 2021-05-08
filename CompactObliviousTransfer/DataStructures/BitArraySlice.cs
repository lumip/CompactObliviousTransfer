using System;
using System.Collections.Generic;
using System.Linq;

namespace CompactOT.DataStructures
{

    public class BitArraySlice : BitArrayBase
    {

        BitArrayBase _array;
        int _start;
        int _stopBefore;

        public BitArraySlice(BitArrayBase array, int start, int stopBefore)
        {
            if (array.Length < stopBefore)
                throw new ArgumentOutOfRangeException($"Bit array slice [{start}, {stopBefore}] exceeds bit array length {array.Length}");
            if (stopBefore <= start)
                throw new ArgumentOutOfRangeException();
            if (start < 0)
                throw new ArgumentOutOfRangeException();

            _array = array;
            _start = start;
            _stopBefore = stopBefore;
        }

        public override int Length => _stopBefore - _start;

        public override bool IsSynchronized => false;

        public override object? SyncRoot => _array.SyncRoot;

        public override bool IsReadOnly => true;

        public override IEnumerable<byte> AsByteEnumerable()
        {
            int byteOffset = _start / 8;
            int bitOffset = _start % 8;
            int lastByteOffset = (_stopBefore - 1) / 8;
            int numberOfBytes = (lastByteOffset + 1 - byteOffset);
            int numberOfOutputBytes = (Length + 7) / 8; // todo: somewhat hacky solution right now, try to make this nicer?
            return new ShiftedByteArrayEnumerable(_array.AsByteEnumerable().Skip(byteOffset).Take(numberOfBytes), bitOffset).Take(numberOfOutputBytes);
        }

        public override IEnumerator<Bit> GetEnumerator()
        {
            return new ByteToBitEnumerable(AsByteEnumerable(), _stopBefore - _start).GetEnumerator();
        }
    }

}