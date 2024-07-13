// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;

namespace CompactOT.DataStructures
{

    /// <summary>
    /// An enumerator of bits from an enumerator of bytes.
    /// 
    /// This enumerator starts proceeds from least to most significant bit in each byte of the byte enumerator.
    /// </summary>
    public class ByteToBitEnumerator : BaseEnumerator<Bit>
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

        public override Bit Current => new Bit((byte)((_byteEnumerator.Current >> (_bitIndex & 0b111)) & 1)); // _bitIndex % 8 == 0

        public override void Dispose()
        {
            _byteEnumerator.Dispose();
        }

        public override bool MoveNext()
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

        public override void Reset()
        {
            _byteEnumerator.Reset();
            _bitIndex = -1;
        }
    }

}
