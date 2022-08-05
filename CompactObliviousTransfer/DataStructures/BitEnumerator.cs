using System;
using System.Collections;
using System.Collections.Generic;

namespace CompactOT.DataStructures
{

    public class BaseEnumeratorExhaustedException : Exception
    {
        public BaseEnumeratorExhaustedException() : base("The base enumerator was exhausted will while derived enumerator expects more elements.")
        {
            
        }
    }

    /// <summary>
    /// An enumerator of bits from an enumerator of bytes.
    /// 
    /// This enumerator starts proceeds from least to most significant bit in each byte of the byte enumerator.
    /// </summary>
    public class ByteToBitEnumerator : IEnumerator<Bit>
    {

        private int _bitIndex;
        private int? _length;

        private IEnumerator<byte> _byteEnumerator;

        public ByteToBitEnumerator(IEnumerator<byte> byteEnumerator, int length)
        {
            _byteEnumerator = byteEnumerator;
            _bitIndex = -1;
            _length = length;
        }

        public ByteToBitEnumerator(IEnumerator<byte> byteEnumerator)
        {
            _byteEnumerator = byteEnumerator;
            _bitIndex = -1;
            _length = null;
        }

        public Bit Current => new Bit((byte)((_byteEnumerator.Current >> (_bitIndex & 0b111)) & 1)); // _bitIndex % 8 == 0
        object IEnumerator.Current => ((IEnumerator<Bit>)this).Current;

        public void Dispose()
        {
            _byteEnumerator.Dispose();
        }

        public bool MoveNext()
        {
            _bitIndex += 1;
            if (_length != null && _bitIndex >= _length)
                return false;

            if ((_bitIndex & 0b111) == 0) // _bitIndex % 8 == 0
            {
                if (!_byteEnumerator.MoveNext())
                {
                    if (_length != null && _bitIndex < _length)
                    {
                        throw new BaseEnumeratorExhaustedException();
                    }
                    return false;
                }
            }
            return true;
        }

        public void Reset()
        {
            _byteEnumerator.Reset();
            _bitIndex = -1;
        }
    }

    public class ByteToBitEnumerable : IEnumerable<Bit>
    {
        private IEnumerable<byte> _byteEnumerable;
        private int _numberOfBits;

        public ByteToBitEnumerable(IEnumerable<byte> byteEnumerable, int numberOfBits)
        {
            if (numberOfBits < 0)
                throw new ArgumentOutOfRangeException("Number of bits cannot be negative.", nameof(numberOfBits));
            _numberOfBits = numberOfBits;
            _byteEnumerable = byteEnumerable;
        }

        public IEnumerator<Bit> GetEnumerator()
        {
            return new ByteToBitEnumerator(_byteEnumerable.GetEnumerator(), _numberOfBits);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Bit>)this).GetEnumerator();
        }
    }

    /// <summary>
    /// An enumerator of bytes from an enumerator of bits.
    /// 
    /// For each byte it outputs, this enumerator collects 8 bits from the bit enumerator
    /// assembling them as a byte from least to most significant bit. If the number of bits
    /// in the bit enumerator is not a multiple of 8, the last byte output is filled with 0
    /// for the most significant bits.
    /// </summary>
    public class BitToByteEnumerator : IEnumerator<byte>
    {
        private IEnumerator<Bit> _bitEnumerator;
        byte _byte;

        public BitToByteEnumerator(IEnumerator<Bit> bitEnumerator)
        {
            _bitEnumerator = bitEnumerator;
            _byte = 0;
        }

        public byte Current => _byte;

        object IEnumerator.Current => ((IEnumerator<byte>)this).Current;

        public void Dispose()
        {
            _bitEnumerator.Dispose();
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if (!_bitEnumerator.MoveNext())
                return false;

            _byte = 0;
            int i = 0;
            do
            {
                _byte = (byte)((int)_byte | (((byte)_bitEnumerator.Current) << i));
                i++;
            } while(i < 8 && _bitEnumerator.MoveNext());
            
            return true;
        }

        public void Reset()
        {
            _bitEnumerator.Reset();
            _byte = 0;
        }
    }

    public class BitToByteEnumerable : IEnumerable<byte>
    {
        private IEnumerable<Bit> _bitEnumerable;

        public BitToByteEnumerable(IEnumerable<Bit> bitEnumerable)
        {
            _bitEnumerable = bitEnumerable;
        }

        public IEnumerator<byte> GetEnumerator()
        {
            return new BitToByteEnumerator(_bitEnumerable.GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<byte>)this).GetEnumerator();
        }
    }
}