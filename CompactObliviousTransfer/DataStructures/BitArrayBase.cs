using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;

namespace CompactOT.DataStructures
{

    public abstract class BitArrayBase : ICollection, IEnumerable<Bit>
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
        public void CopyTo(byte[] buffer, int offset)
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

        public virtual void CopyTo(Bit[] buffer, int offset)
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

        public virtual BitArrayBase Concatenate(BitArrayBase other)
        {
            return new EnumeratedBitArrayView(
                new BitToByteEnumerable(((IEnumerable<Bit>)this).Concat(other)), Length + other.Length);
        }

        public virtual BitArrayBase Not()
        {
            return new EnumeratedBitArrayView(
                ByteEnumerableOperations.Not(AsByteEnumerable()), Length
            );
        }

        public virtual BitArrayBase And(BitArrayBase other)
        {
            return new EnumeratedBitArrayView(
                ByteEnumerableOperations.And(AsByteEnumerable(), other.AsByteEnumerable()), Length
            );
        }

        public virtual BitArrayBase Or(BitArrayBase other)
        {
            return new EnumeratedBitArrayView(
                ByteEnumerableOperations.Or(AsByteEnumerable(), other.AsByteEnumerable()), Length
            );
        }
        public virtual BitArrayBase Xor(BitArrayBase other)
        {
            return new EnumeratedBitArrayView(
                ByteEnumerableOperations.Xor(AsByteEnumerable(), other.AsByteEnumerable()), Length
            );
        }

        public virtual byte[] ToBytes()
        {
            int bufferSize = BitArray.RequiredBytes(Length);
            byte[] buffer = new byte[bufferSize];
            CopyTo(buffer, 0);
            return buffer;
        }

        public virtual BitArrayBase And(Bit bit) => And(new ConstantBitArrayView(bit, Length));
        public virtual BitArrayBase Or(Bit bit) => Or(new ConstantBitArrayView(bit, Length));
        public virtual BitArrayBase Xor(Bit bit) => Xor(new ConstantBitArrayView(bit, Length));

        public static BitArrayBase operator &(BitArrayBase left, BitArrayBase right) => left.And(right);
        public static BitArrayBase operator |(BitArrayBase left, BitArrayBase right) => left.Or(right);
        public static BitArrayBase operator ^(BitArrayBase left, BitArrayBase right) => left.Xor(right);

        public static BitArrayBase operator ~(BitArrayBase left) => left.Not();

        public static BitArrayBase operator &(BitArrayBase left, Bit right) => left.And(right);
        public static BitArrayBase operator &(Bit left, BitArrayBase right) => right.And(left);

        public static BitArrayBase operator |(BitArrayBase left, Bit right) => left.Or(right);
        public static BitArrayBase operator |(Bit left, BitArrayBase right) => right.Or(left);

        public static BitArrayBase operator ^(BitArrayBase left, Bit right) => left.Xor(right);
        public static BitArrayBase operator ^(Bit left, BitArrayBase right) => right.Xor(left);
    }
}