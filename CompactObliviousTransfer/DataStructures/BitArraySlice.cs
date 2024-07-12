// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using CompactCryptoGroupAlgebra;

namespace CompactOT.DataStructures
{

    public class BitArraySlice : BitSequence
    {

        BitSequence _array;
        int _start;
        int _stopBefore;

        public BitArraySlice(BitSequence array, int start, int stopBefore)
        {
            if (array.Length < stopBefore)
                throw new ArgumentOutOfRangeException($"Bit array slice [{start}, {stopBefore}] exceeds bit array length {array.Length}.");
            if (stopBefore <= start)
                throw new ArgumentOutOfRangeException($"Start {start} must be before stop {stopBefore}.");
            if (start < 0)
                throw new ArgumentOutOfRangeException($"Start must not be negative; was {start}.");

            _array = array;
            _start = start;
            _stopBefore = stopBefore;
        }

        public override int Length => _stopBefore - _start;

        public override bool IsSynchronized => false;

        public override object? SyncRoot => _array.SyncRoot;

        public override bool IsReadOnly => true;

        /// <summary>
        /// Returns a byte enumerable of the content of this slice, i.e.,
        /// starting at the desired start bit as indicated by the offset.
        /// However, the last byte in the returned enumerable includes extra
        /// bits at the end if the the Length of the slice is not a multiple
        /// of 8, i.e., superfluous end bits are not masked out.
        /// </summary>
        private IEnumerable<byte> AsByteEnumerableInternalWithExtraTrailing()
        {
            int byteOffset = _start / 8;
            int bitOffset = _start % 8;
            int lastByteOffset = (_stopBefore - 1) / 8;
            int numberOfBytes = lastByteOffset + 1 - byteOffset;
            
            int numberOfOutputBytes = NumberLength.FromBitLength(Length).InBytes;
            var unfilteredByteEnumerable = new ShiftedByteArrayEnumerable(
                _array.AsByteEnumerable().Skip(byteOffset).Take(numberOfBytes),
                bitOffset
            ).Take(numberOfOutputBytes);

            return unfilteredByteEnumerable;
        }

        public override IEnumerable<byte> AsByteEnumerable()
        {
            var unfilteredByteEnumerable = AsByteEnumerableInternalWithExtraTrailing();

            return new EnumeratedBitArrayView(unfilteredByteEnumerable, Length).AsByteEnumerable();
        }

        public override IEnumerator<Bit> GetEnumerator()
        {
            return new ByteToBitEnumerable(AsByteEnumerableInternalWithExtraTrailing(), _stopBefore - _start).GetEnumerator();
        }
    }

}