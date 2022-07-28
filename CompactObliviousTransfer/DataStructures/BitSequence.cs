using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;

namespace CompactOT.DataStructures
{

    public abstract class BitSequence : ICollection, IEnumerable<Bit>
    {

        public abstract int Length { get; }

        public int Count => Length;

        public abstract bool IsSynchronized { get; }

        public abstract object? SyncRoot { get; }

        public abstract bool IsReadOnly { get; }

        public abstract IEnumerable<byte> AsByteEnumerable();
        public abstract IEnumerator<Bit> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Bit>)this).GetEnumerator();
        }

        protected virtual void CopyToInternal(byte[] buffer, int offset)
        {
            AsByteEnumerable().WriteInto(buffer, offset);
        }
        
        public void CopyTo(byte[] buffer, int offset = 0)
        {
            CopyToInternal(buffer, offset);
            var nonAlignedBits = Length % 8;
            if (nonAlignedBits > 0)
            {
                var mask = (byte)~(0xff << nonAlignedBits);
                var numBytes = BitArray.RequiredBytes(Length);
                buffer[offset + (numBytes - 1)] &= mask;
            }
        }

        public virtual void CopyTo(Bit[] buffer, int offset = 0)
        {
            ((IEnumerable<Bit>)this).WriteInto(buffer, offset);
        }

        public virtual void CopyTo(Array array, int index)
        {
            if (array is Bit[])
            {
                CopyTo((Bit[])array, index);
            }
            else if (array is byte[])
            {
                CopyTo((byte[])array, index);
            }
            else
            {
                throw new NotSupportedException("Copying to any array other than Bit[] or byte[] is not supported.");
            }
        }

        public virtual string ToBinaryString()
        {
            return String.Concat(((IEnumerable<Bit>)this).Select(b => b ? '1' : '0'));
        }

        public virtual IEnumerable<int> ToSelectionIndices()
        {
            return ((IEnumerable<Bit>)this).Select(b => (int)b);
        }

        public virtual BitSequence Concatenate(BitSequence other)
        {
            return new EnumeratedBitArrayView(
                new BitToByteEnumerable(((IEnumerable<Bit>)this).Concat(other)), Length + other.Length);
        }

        public virtual BitSequence Not()
        {
            return new EnumeratedBitArrayView(
                ByteEnumerableOperations.Not(AsByteEnumerable()), Length
            );
        }

        public virtual BitSequence And(BitSequence other)
        {
            return new EnumeratedBitArrayView(
                ByteEnumerableOperations.And(AsByteEnumerable(), other.AsByteEnumerable()), Length
            );
        }

        public virtual BitSequence Or(BitSequence other)
        {
            return new EnumeratedBitArrayView(
                ByteEnumerableOperations.Or(AsByteEnumerable(), other.AsByteEnumerable()), Length
            );
        }
        public virtual BitSequence Xor(BitSequence other)
        {
            return new EnumeratedBitArrayView(
                ByteEnumerableOperations.Xor(AsByteEnumerable(), other.AsByteEnumerable()), Length
            );
        }

        public virtual byte[] ToBytes()
        {
            int bufferSize = BitArray.RequiredBytes(Length);
            byte[] buffer = new byte[bufferSize];
            CopyTo(buffer);
            return buffer;
        }

        public virtual BitSequence And(Bit bit) => And(new ConstantBitArrayView(bit, Length));
        public virtual BitSequence Or(Bit bit) => Or(new ConstantBitArrayView(bit, Length));
        public virtual BitSequence Xor(Bit bit) => Xor(new ConstantBitArrayView(bit, Length));

        public static BitSequence operator &(BitSequence left, BitSequence right) => left.And(right);
        public static BitSequence operator |(BitSequence left, BitSequence right) => left.Or(right);
        public static BitSequence operator ^(BitSequence left, BitSequence right) => left.Xor(right);

        public static BitSequence operator ~(BitSequence left) => left.Not();

        public static BitSequence operator &(BitSequence left, Bit right) => left.And(right);
        public static BitSequence operator &(Bit left, BitSequence right) => right.And(left);

        public static BitSequence operator |(BitSequence left, Bit right) => left.Or(right);
        public static BitSequence operator |(Bit left, BitSequence right) => right.Or(left);

        public static BitSequence operator ^(BitSequence left, Bit right) => left.Xor(right);
        public static BitSequence operator ^(Bit left, BitSequence right) => right.Xor(left);
    }
}