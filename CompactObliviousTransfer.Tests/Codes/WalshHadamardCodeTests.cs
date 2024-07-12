// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
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
            Assert.Equal(0, WalshHadamardCode.GetParity(0x59959C17));
            Assert.Equal(1, WalshHadamardCode.GetParity(0x19959C17));
        }

        [Theory]
        [InlineData(31, "1001011001101001")]
        [InlineData(18, "0101010110101010")]
        [InlineData(15, "1001011010010110")]
        [InlineData(12, "0011110000111100")]
        [InlineData(7,  "1001100110011001")]
        [InlineData(1,  "1111111111111111")]
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
            Assert.Throws<ArgumentException>(() => new WalshHadamardCode(0));
            Assert.Throws<ArgumentException>(() => new WalshHadamardCode(1));
        }

        [Fact]
        public void TestCreateWithDistance()
        {
            int distance = 7;
            var code = WalshHadamardCode.CreateWithDistance(distance);
            Assert.True(code.Distance >= distance);

            int expectedDistance = 8;
            int expectedCodeLength = 16;
            int expectedMaximumMessage = 31;
            Assert.Equal(expectedDistance, code.Distance);
            Assert.Equal(expectedCodeLength, code.CodeLength);
            Assert.Equal(expectedMaximumMessage, code.MaximumMessage);
        }

        [Fact]
        public void TestCreateWithDistanceTooLargeDistance()
        {
            int distance = 1 << 30;
            Assert.Throws<ArgumentOutOfRangeException>(
                () => WalshHadamardCode.CreateWithDistance(distance)
            );
        }

        [Fact]
        public void TestCreateWithMaximumMessage()
        {
            int maximumMessage = 6;
            var code = WalshHadamardCode.CreateWithMaximumMessage(maximumMessage);
            Assert.True(code.MaximumMessage >= maximumMessage);

            int expectedMaximumMessage = 7;
            int expectedCodeLength = 4;
            int expectedDistance = 2;
            Assert.Equal(expectedDistance, code.Distance);
            Assert.Equal(expectedCodeLength, code.CodeLength);
            Assert.Equal(expectedMaximumMessage, code.MaximumMessage);
        }

    }
}
