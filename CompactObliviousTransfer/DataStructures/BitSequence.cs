// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Numerics;

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

        public static BitSequence Empty = new EmptyBitSequence();

        protected virtual void CopyToInternal(byte[] buffer, int offset)
        {
            AsByteEnumerable().WriteInto(buffer, offset);
        }
        
        /// <summary>
        /// Copies the bit sequence into a given buffer.
        /// 
        /// The buffer is filled from low to high indices (starting at the specified byte offset)
        /// and each byte is filled from least to most significant bit.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        public void CopyTo(byte[] buffer, int offset = 0)
        {
            var numBytes = BitArray.RequiredBytes(Length);
            if (buffer.Length < numBytes + offset)
            {
                throw new ArgumentException("The target buffer is too small. " + 
                    $"It has length {buffer.Length} but need to write {numBytes} with {offset} bytes offset.",
                    nameof(buffer)
                );
            }

            CopyToInternal(buffer, offset);
            var nonAlignedBits = Length % 8;
            if (nonAlignedBits > 0)
            {
                var mask = (byte)~(0xff << nonAlignedBits);
                buffer[offset + (numBytes - 1)] &= mask;
            }
        }

        public virtual void CopyTo(Bit[] buffer, int offset = 0)
        {
            if (buffer.Length < Length + offset)
            {
                throw new ArgumentException("The target buffer is too small. " + 
                    $"It has length {buffer.Length} but need to write {Length} with {offset} bits offset.",
                    nameof(buffer)
                );
            }
            
            ((IEnumerable<Bit>)this).WriteInto(buffer, offset);
        }

        public virtual void CopyTo(Array array, int offset)
        {
            if (array is Bit[])
            {
                CopyTo((Bit[])array, offset);
            }
            else if (array is byte[])
            {
                CopyTo((byte[])array, offset);
            }
            else
            {
                throw new NotSupportedException("Copying to any array other than Bit[] or byte[] is not supported.");
            }
        }

        public virtual byte[] ToBytes()
        {
            int bufferSize = BitArray.RequiredBytes(Length);
            byte[] buffer = new byte[bufferSize];
            CopyTo(buffer);
            return buffer;
        }

        public virtual int ToInt32()
        {
            if (Length > 32)
                throw new InvalidOperationException($"Can only convert a BitSequence to an integer if it contains no more than 32 bits, but had {Length} bits.");

            return BitConverter.ToInt32(ToBytes(), 0);
        }

        public virtual BigInteger ToBigInteger(bool unsigned = false)
        {
            int bufferSize = BitArray.RequiredBytes(Length);
            byte[] buffer;
            if (unsigned)
            {
                buffer = new byte[bufferSize + 1];
                buffer[bufferSize] = 0;
            }
            else
            {
                buffer = new byte[bufferSize];
            }

            CopyTo(buffer);

            return new BigInteger(buffer);
        }

        public virtual string ToBinaryString()
        {
            return String.Concat(((IEnumerable<Bit>)this).Select(b => b ? '1' : '0'));
        }

        public override string ToString()
        {
            return ToBinaryString();
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

        public override bool Equals(object obj)
        {
            BitSequence? other = obj as BitSequence;
            if (other == null) return false;

            return AsByteEnumerable().SequenceEqual(other.AsByteEnumerable());
        }

        public override int GetHashCode()
        {
            int hash = 3697;

            foreach (var b in AsByteEnumerable())
            {
                hash = hash * 5573 + b;
            }
            return hash;
        }

        public virtual bool IsZero
        {
            get
            {
                byte aggregated = AsByteEnumerable().Aggregate((a, b) => (byte)(b | a));
                return aggregated == 0;
            }
        }
    }

}
