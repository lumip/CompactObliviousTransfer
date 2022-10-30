// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using Xunit;
using System;

using CompactOT.DataStructures;

namespace CompactOT.Codes
{
    public class WalshHadamardCodeTests
    {

        [Fact]
        public void TestGetParity()
        {
            Assert.Equal(0, WalshHadamardCode.GetParity(0b101));
            Assert.Equal(1, WalshHadamardCode.GetParity(0b1110011));
            Assert.Equal(0, WalshHadamardCode.GetParity(((int)0x59959C17)));
            Assert.Equal(1, WalshHadamardCode.GetParity(((int)0x19959C17)));
        }

        [Theory]
        [InlineData(15, "0110100110010110")]
        [InlineData(12, "0000111111110000")]
        [InlineData(7,  "0110100101101001")]
        [InlineData(0,  "0000000000000000")]
        public void TestEncode(int value, string expectedString)
        {
            var code = new WalshHadamardCode(16);

            var expected = BitArray.FromBinaryString(expectedString);
            var result = code.Encode(value);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestEncodeRejectsTooLargeValue()
        {
            var code = new WalshHadamardCode(4);
            Assert.Throws<ArgumentOutOfRangeException>(() => code.Encode(0b10000));
        }

        [Fact]
        public void TestConstructorRejectsInvalidCodeLength()
        {
            Assert.Throws<ArgumentException>(() => new WalshHadamardCode(3));
        }

        [Fact]
        public void TestCreateWithDistance()
        {
            int distance = 7;
            var code = WalshHadamardCode.CreateWithDistance(distance);
            int expectedDistance = 8;
            int expectedCodeLength = 16;
            int expectedMaximumMessage = 15;
            Assert.Equal(expectedDistance, code.Distance);
            Assert.Equal(expectedCodeLength, code.CodeLength);
            Assert.Equal(expectedMaximumMessage, code.MaximumMessage);
        }

        [Fact]
        public void TestCreateWithMaximumMessage()
        {
            int maximumMessage = 6;
            var code = WalshHadamardCode.CreateWithMaximumMessage(maximumMessage);
            int expectedMaximumMessage = 7;
            int expectedCodeLength = 8;
            int expectedDistance = 4;
            Assert.Equal(expectedDistance, code.Distance);
            Assert.Equal(expectedCodeLength, code.CodeLength);
            Assert.Equal(expectedMaximumMessage, code.MaximumMessage);
        }

    }
}
