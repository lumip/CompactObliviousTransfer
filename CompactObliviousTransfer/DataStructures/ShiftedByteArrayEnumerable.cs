// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections;
using System.Collections.Generic;

namespace CompactOT.DataStructures
{

    /// <summary>
    /// Interprets a sequence of bytes as representing a little Endian bit sequence
    /// and shifts it to the left by a given number of sub-byte positions.
    ///
    /// The first bit in the incoming bit sequence is the lsb of the first byte,
    /// the 9th bit is the lsb of the second byte and so on.
    /// </summary>
    public class ShiftedByteArrayEnumerable : IEnumerable<byte>
    {
        public class Enumerator : IEnumerator<byte>
        {
            private IEnumerator<byte> _baseEnumerator;
            private int _offset;

            private byte _last;

            private bool _isReset;
            private bool _hasEnded;

            public Enumerator(IEnumerator<byte> enumerator, int offset)
            {
                if (offset < 0 || offset >= 8)
                    throw new ArgumentOutOfRangeException("offset must be between 0 and 7.", nameof(offset));

                _baseEnumerator = enumerator;
                _offset = offset;
                _isReset = true;
                _last = 0;
                _hasEnded = false;
            }


            public byte Current { get; private set; }

            object IEnumerator.Current => ((IEnumerator<byte>)this).Current;

            public void Dispose()
            {
                _baseEnumerator.Dispose();
            }

            public bool MoveNext()
            {
                if (_hasEnded) return false;

                if (_isReset)
                {
                    if (!_baseEnumerator.MoveNext()) return false;
                    _isReset = false;
                    _last = (byte)(_baseEnumerator.Current >> _offset);
                }

                if (_baseEnumerator.MoveNext())
                {
                    byte baseCurrent = _baseEnumerator.Current;
                    Current = (byte)(_last | (baseCurrent << (8 - _offset)));
                    _last = (byte)(baseCurrent >> _offset);
                }
                else
                {
                    Current = _last;
                    _hasEnded = true;
                }
                return true;
            }

            public void Reset()
            {
                _isReset = true;
                _hasEnded = false;
                _baseEnumerator.Reset();
            }
        }

        private IEnumerable<byte> _baseEnumerable;
        private int _offset;

        public ShiftedByteArrayEnumerable(IEnumerable<byte> baseEnumerable, int offset)
        {
            if (offset < 0 || offset >= 8)
                throw new ArgumentOutOfRangeException("offset must be between 0 and 7.", nameof(offset));

            _baseEnumerable = baseEnumerable;
            _offset = offset;
        }

        public IEnumerator<byte> GetEnumerator()
        {
            return new Enumerator(_baseEnumerable.GetEnumerator(), _offset);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<byte>)this).GetEnumerator();
        }
    }

}