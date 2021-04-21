using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;

namespace CompactOT.DataStructures
{

    public abstract class BitArrayBase : IBitArray
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
            var mask = (byte)~(0xff << nonAlignedBits);
            var numBytes = BitArray.RequiredBytes(Length);
            buffer[offset + (numBytes - 1)] &= mask;
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

        public virtual IBitArray Concatenate(IBitArray other)
        {
            return new EnumeratedBitArrayView(
                new BitToByteEnumerable(((IEnumerable<Bit>)this).Concat(other)), Length + other.Length);
        }

        public virtual IBitArray Not()
        {
            return new EnumeratedBitArrayView(
                ByteEnumerableOperations.Not(AsByteEnumerable()), Length
            );
        }

        public virtual IBitArray And(IBitArray other)
        {
            return new EnumeratedBitArrayView(
                ByteEnumerableOperations.And(AsByteEnumerable(), other.AsByteEnumerable()), Length
            );
        }

        public virtual IBitArray Or(IBitArray other)
        {
            return new EnumeratedBitArrayView(
                ByteEnumerableOperations.Or(AsByteEnumerable(), other.AsByteEnumerable()), Length
            );
        }
        public virtual IBitArray Xor(IBitArray other)
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

        public virtual IBitArray And(Bit bit) => And(new ConstantBitArrayView(bit, Length));
        public virtual IBitArray Or(Bit bit) => Or(new ConstantBitArrayView(bit, Length));
        public virtual IBitArray Xor(Bit bit) => Xor(new ConstantBitArrayView(bit, Length));

        public static IBitArray operator &(BitArrayBase left, BitArrayBase right) => left.And(right);
        public static IBitArray operator |(BitArrayBase left, BitArrayBase right) => left.Or(right);
        public static IBitArray operator ^(BitArrayBase left, BitArrayBase right) => left.Xor(right);

        public static IBitArray operator &(BitArrayBase left, IBitArray right) => left.And(right);
        public static IBitArray operator |(BitArrayBase left, IBitArray right) => left.Or(right);
        public static IBitArray operator ^(BitArrayBase left, IBitArray right) => left.Xor(right);
        public static IBitArray operator ~(BitArrayBase left) => left.Not();

        public static IBitArray operator &(IBitArray left, BitArrayBase right) => right.And(left);
        public static IBitArray operator |(IBitArray left, BitArrayBase right) => right.Or(left);
        public static IBitArray operator ^(IBitArray left, BitArrayBase right) => right.Xor(left);

        public static IBitArray operator &(BitArrayBase left, Bit right) => left.And(right);
        public static IBitArray operator &(Bit left, BitArrayBase right) => right.And(left);

        public static IBitArray operator |(BitArrayBase left, Bit right) => left.Or(right);
        public static IBitArray operator |(Bit left, BitArrayBase right) => right.Or(left);

        public static IBitArray operator ^(BitArrayBase left, Bit right) => left.Xor(right);
        public static IBitArray operator ^(Bit left, BitArrayBase right) => right.Xor(left);
    }
}