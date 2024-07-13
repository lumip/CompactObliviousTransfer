// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;

namespace CompactOT.DataStructures
{

    /// <summary>
    /// An enumerator of bytes from an enumerator of bits.
    /// 
    /// For each byte it outputs, this enumerator collects 8 bits from the bit enumerator,
    /// assembling them as a byte from least to most significant bit. If the number of bits
    /// in the bit enumerator is not a multiple of 8, the last byte output is filled with 0
    /// for the most significant bits.
    /// </summary>
    public class BitToByteEnumerator : BaseEnumerator<byte>
    {
        private IEnumerator<Bit> _bitEnumerator;
        byte _byte;
        bool _isReset;

        public BitToByteEnumerator(IEnumerator<Bit> bitEnumerator)
        {
            _bitEnumerator = bitEnumerator;
            _byte = 0;
            _isReset = true;
        }

        public override byte Current
        {
            get
            {
                if (_isReset)
                    throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
                return _byte;
            }
        }

        public override void Dispose()
        {
            _bitEnumerator.Dispose();
        }

        /// <inheritdoc/>
        public override bool MoveNext()
        {
            if (!_bitEnumerator.MoveNext())
                return false;

            _isReset = false;
            _byte = 0;

            int i = 0;
            do
            {
                _byte = (byte)((int)_byte | (((byte)_bitEnumerator.Current) << i));
                i++;
            } while (i < 8 && _bitEnumerator.MoveNext());

            return true;
        }

        public override void Reset()
        {
            _bitEnumerator.Reset();
            _byte = 0;
            _isReset = true;
        }
    }

}
