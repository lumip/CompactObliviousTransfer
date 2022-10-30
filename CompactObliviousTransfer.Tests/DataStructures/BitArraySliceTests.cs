// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Linq;

using Xunit;

namespace CompactOT.DataStructures
{
    public class BitArraySliceTests
    {
        [Theory]
        [InlineData(3, 13, "1100110101")]
        [InlineData(5, 10, "00110")]
        [InlineData(6, 24, "011010110111010111")]
        [InlineData(8, 16, "10101101")]
        [InlineData(0, 28, "0011100110101101110101111001")]
        [InlineData(1, 3, "01")]
        [InlineData(7, 10, "110")]
        [InlineData(21, 22, "1")]
        [InlineData(26, 28, "01")]
        public void TestSlices(int sliceOffset, int sliceStop, string expected)
        {
            byte[] bytes = new byte[] { 0x9c, 0xb5, 0xeb, 0x69 }; // 10011100 10110101 11101011 01101001
            var bits = BitArray.FromBytes(bytes, 28); // "0011100110101101110101111001"
            var slice = new BitArraySlice(bits, sliceOffset, sliceStop);

            var expectedSlicedBits = BitArray.FromBinaryString(expected);
            
            foreach ((Bit expectedBit, Bit bit) in expectedSlicedBits.Zip(slice))
            {
                Assert.Equal(expectedBit, bit);
            }
            Assert.Equal(sliceStop - sliceOffset, slice.Length);
        }

        [Fact]
        public void TestRejectNegativeStarts()
        {
            byte[] bytes = new byte[] { 0x9c, 0xb5, 0xeb, 0x69 };
            var bits = BitArray.FromBytes(bytes, 28);
            Assert.Throws<ArgumentOutOfRangeException>(() => new BitArraySlice(bits, -1, 1));
        }

        [Fact]
        public void TestRejectStopOutsideBounds()
        {
            byte[] bytes = new byte[] { 0x9c, 0xb5, 0xeb, 0x69 };
            var bits = BitArray.FromBytes(bytes, 28);
            Assert.Throws<ArgumentOutOfRangeException>(() => new BitArraySlice(bits, 4, 29));
        }

        [Fact]
        public void TestRejectStopBeforeStart()
        {
            byte[] bytes = new byte[] { 0x9c, 0xb5, 0xeb, 0x69 };
            var bits = BitArray.FromBytes(bytes, 28);
            Assert.Throws<ArgumentOutOfRangeException>(() => new BitArraySlice(bits, 5, 4));
        }

        [Theory]
        [InlineData(0, 8, new byte[] { 0x9c })]
        [InlineData(8, 16, new byte[] { 0xb5 })]
        [InlineData(3, 11, new byte[] { 0b10110011 })]
        [InlineData(0, 7, new byte[] { 0x1c })]
        [InlineData(0, 28, new byte[] { 0x9c, 0xb5, 0xeb, 0x09 })]
        [InlineData(21, 22, new byte[] { 0x01 })]
        [InlineData(26, 28, new byte[] { 0x02 })]
        public void TestAsByteEnumerable(int sliceOffset, int sliceStop, byte[] expected)
        {
            byte[] bytes = new byte[] { 0x9c, 0xb5, 0xeb, 0x69 }; // 10011100 10110101 11101011 01101001
            var bits = BitArray.FromBytes(bytes, 28); // "0011100110101101110101111001"
            var slice = new BitArraySlice(bits, sliceOffset, sliceStop);

            var bbytes = slice.AsByteEnumerable().ToArray();

            Assert.Equal(expected.Length, slice.AsByteEnumerable().Count());
            Assert.Equal(expected, slice.AsByteEnumerable());
        }
    }
}