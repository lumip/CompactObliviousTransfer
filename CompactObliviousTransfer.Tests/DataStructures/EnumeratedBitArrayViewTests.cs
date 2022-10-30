// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;

namespace CompactOT.DataStructures
{
    public class EnumeratedBitArrayViewTests
    {
        [Fact]
        public void TestConstruction()
        {
            byte[] bytes = new byte[] { 0x56, 0x8d, 0xa3 }; //   01010110 10001101 10100011
            var bits = new EnumeratedBitArrayView(bytes, 11); // 01010110      101
            Assert.Equal(11, bits.Length);

            var expectedBits = BitArray.FromBinaryString("01101010 101");
            Assert.Equal(bits, expectedBits);
        }

        [Fact]
        public void TestAsByteEnumerable()
        {
            byte[] bytes = new byte[] { 0x56, 0x8d, 0xa3 };
            var bits = new EnumeratedBitArrayView(bytes, 11);

            var bitsAsArray = bits.AsByteEnumerable().ToArray();
            var expectedBitsAsArray = new byte[] { 0x56, 0x05 };
            Assert.Equal(expectedBitsAsArray, bitsAsArray);
        }
    }
}