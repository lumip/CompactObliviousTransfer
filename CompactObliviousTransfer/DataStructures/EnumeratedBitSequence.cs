using System.Collections.Generic;

namespace CompactOT.DataStructures
{
    public class EnumeratedBitSequence : BitSequence
    {
        private IEnumerable<Bit> _bitEnumerable;

        public EnumeratedBitSequence(IEnumerable<Bit> bitEnumerable, int length)
        {
            _bitEnumerable = bitEnumerable;
            Length = length;
        }

        public override int Length { get; }

        public override bool IsSynchronized => false;

        public override object? SyncRoot => false;

        public override bool IsReadOnly => true;

        public override IEnumerable<byte> AsByteEnumerable()
        {
            return new BitToByteEnumerable(_bitEnumerable);
        }

        public override IEnumerator<Bit> GetEnumerator()
        {
            return _bitEnumerable.GetEnumerator();
        }
    }
}
