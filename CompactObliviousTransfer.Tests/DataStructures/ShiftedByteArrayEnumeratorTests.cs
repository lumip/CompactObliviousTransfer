// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace CompactOT.DataStructures
{
    public class ShiftedByteArrayEnumerableTests
    {
        
        [Fact]
        public void TestLongInput()
        {
            var bits = BitArray.FromBinaryString("0110 0010  0101 0111  0110 1110  1101 0100").ToBytes();
            int offset = 5;

            var expectedBuffer = BitArray.FromBinaryString("0100 1010  1110 1101  1101 1010  1000 0000").ToBytes();

            var shiftedEnumerable = new ShiftedByteArrayEnumerable(bits, offset);
            var resultBuffer = shiftedEnumerable.ToArray();

            Assert.Equal(expectedBuffer, resultBuffer);
        }

        [Fact]
        public void TestSingleByte()
        {
            var bits = BitArray.FromBinaryString("0110 0010").ToBytes();
            int offset = 2;

            var expectedBuffer = BitArray.FromBinaryString("1000 1000").ToBytes();

            var shiftedEnumerable = new ShiftedByteArrayEnumerable(bits, offset);
            var resultBuffer = shiftedEnumerable.ToArray();

            Assert.Equal(expectedBuffer, resultBuffer);
        }

        [Fact]
        public void TestEmpty()
        {
            var bits = new byte[0];
            int offset = 2;

            var expectedBuffer = new byte[0];

            var shiftedEnumerable = new ShiftedByteArrayEnumerable(bits, offset);
            var resultBuffer = shiftedEnumerable.ToArray();

            Assert.Equal(expectedBuffer, resultBuffer);
        }

        [Fact]
        public void TestEnumeratorCurrent()
        {
            var bits = new byte[] { 0b01010101, 0b11001100 };
            int offset = 3;

            byte expected = 0b10001010;
            var enumerator = new ShiftedByteArrayEnumerable.Enumerator(((IEnumerable<byte>)bits).GetEnumerator(), offset);
            Assert.True(enumerator.MoveNext());

            Assert.Equal(expected, enumerator.Current);
            Assert.Equal(expected, enumerator.Current);
        }

    }
}