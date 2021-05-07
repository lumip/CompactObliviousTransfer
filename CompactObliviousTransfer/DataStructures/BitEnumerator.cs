using System;
using System.Collections;
using System.Collections.Generic;

namespace CompactOT.DataStructures
{
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
                return _byteEnumerator.MoveNext();
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
            if (_bitEnumerator.MoveNext())
                return false;

            _byte = 0;
            int i = 8;
            do
            {
                i -= 1;
                _byte = (byte)((int)_byte | (((byte)_bitEnumerator.Current) << i));
            } while (i >= 0 && _bitEnumerator.MoveNext());
            
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