// SPDX-FileCopyrightText: 2018 Jonas Nagy-Kuhlen <jonas.nagy-kuhlen@rwth-aachen.de>
// SPDX-License-Identifier: MIT
// Adopted from CompactMPC: https://github.com/jnagykuhlen/CompactMPC

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using CompactCryptoGroupAlgebra;

namespace CompactOT.DataStructures
{

    public class BitArray : BitSequence, IReadOnlyList<Bit> // it is also a IList but does not support any operations changing length...
    {

        protected byte[] Buffer { get; }
        public override int Length { get; }
        protected const int ElementsPerByte = 8;

        public BitArray(int numberOfElements)
        {
            if (Length < 0) throw new ArgumentException("numberOfElements cannot be a negative number.");
            Length = numberOfElements;
            Buffer = new byte[RequiredBytes(Length)];
        }

        public BitArray(Bit[] elements)
        {
            Length = elements.Length;
            Buffer = new BitToByteEnumerable(elements).ToArray();
        }

        public BitArray(BitSequence elements)
            : this(elements.Length)
        {
            Debug.Assert(Buffer.Length == elements.AsByteEnumerable().Count());
            elements.AsByteEnumerable().WriteInto(Buffer, 0);
        }

        protected BitArray(byte[] bytes, int numberOfElements)
        {
            Length = numberOfElements;
            Buffer = bytes;
        }

        public static BitArray FromBytes(byte[] bytes, int numberOfElements, int bytesOffset = 0)
        {
            byte[] buffer = new byte[RequiredBytes(numberOfElements)];
            Array.Copy(bytes, bytesOffset, buffer, 0, buffer.Length);
            return new BitArray(buffer, numberOfElements);
        }

        public static BitArray FromBytes(IEnumerable<byte> enumerable, int numberOfElements)
        {
            return FromBytes(enumerable.GetEnumerator(), numberOfElements);
        }

        public static BitArray FromBytes(IEnumerator<byte> enumerator, int numberOfElements)
        {
            int numberOfBytes = RequiredBytes(numberOfElements);
            byte[] buffer = new byte[numberOfBytes];

            for (int i = 0; i < numberOfBytes && enumerator.MoveNext(); ++i)
            {
                buffer[i] = enumerator.Current;
            }
            return FromBytes(buffer, numberOfElements);
        }

        public static int RequiredBytes(int numberOfBits)
        {
            return NumberLength.FromBitLength(numberOfBits).InBytes;
        }

        protected override void CopyToInternal(byte[] buffer, int offset)
        {
            Array.Copy(Buffer, 0, buffer, offset, Buffer.Length);
        }

        public override IEnumerable<byte> AsByteEnumerable()
        {
            return Buffer.AsEnumerable();
        }

        public static BitArray FromBinaryString(string bitString)
        {
            BitArray result = new BitArray(bitString.Where(c => c != ' ').Count());
            foreach ((int i, char c) in bitString.Where(c => c != ' ').Enumerate())
            {
                if (c != '0' && c != '1')
                    throw new ArgumentException("Binary string is only allowed to contain characters 0 and 1.", nameof(bitString));

                result[i] = new Bit(c == '1');
            }

            return result;
        }

        public override string ToString()
        {
            return ToBinaryString();
        }

        public BitArray Clone()
        {
            BitArray clone = new BitArray(Length);
            Array.Copy(Buffer, clone.Buffer, Buffer.Length);
            return clone;
        }

        public override IEnumerator<Bit> GetEnumerator()
        {
            return new ByteToBitEnumerator(AsByteEnumerable().GetEnumerator(), Length);
        }

        public class InPlaceBitArrayOperations
        {
            private BitArray _bitArray;

            internal InPlaceBitArrayOperations(BitArray bitArray)
            {
                _bitArray = bitArray;
            }

            public void Or(BitSequence other)
            {
                ByteEnumerableOperations.InPlaceOr(_bitArray.Buffer, other.AsByteEnumerable());
            }

            public void Xor(BitSequence other)
            {
                ByteEnumerableOperations.InPlaceXor(_bitArray.Buffer, other.AsByteEnumerable());
            }

            public void And(BitSequence other)
            {
                ByteEnumerableOperations.InPlaceAnd(_bitArray.Buffer, other.AsByteEnumerable());
            }

            public void Not()
            {
                ByteEnumerableOperations.InPlaceNot(_bitArray.Buffer);
            }
        }

        public InPlaceBitArrayOperations InPlace => new InPlaceBitArrayOperations(this);

        public override bool IsSynchronized => false;

        public override object? SyncRoot => this;

        public override bool IsReadOnly => false;

        public Bit this[int index]
        {
            get
            {
                return new Bit((byte)(Buffer[index / 8] >> (index % 8)));
            }
            set
            {
                int bitOffset = index % 8;
                byte v = (byte)((byte)value << bitOffset);
                byte mask = (byte)(~(1 << bitOffset));
                int bufferOffset = index / 8;
                Buffer[bufferOffset] = (byte)(Buffer[bufferOffset] & mask | v);
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            BitArray? other = obj as BitArray;
            if (other == null) return false;
            if (other.Length != Length) return false;
            return other.AsByteEnumerable().SequenceEqual(this.AsByteEnumerable());
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Buffer.GetHashCode();
        }
    }
}
