// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Numerics;

using Xunit;
using Moq;

namespace CompactOT.DataStructures
{
    public class BitArrayTests
    {
        [Fact]
        public void TestFromBinaryString()
        {
            var bits = BitArray.FromBinaryString("01110100 01011");
            Assert.Equal(13, bits.Length);

            byte[] expectedBytes = new byte[] { 0x2E, 0x1A };
            byte[] bitsAsBytes = bits.ToBytes();

            Assert.Equal(expectedBytes, bitsAsBytes);

            Assert.Equal(bits[0], Bit.Zero);
            Assert.Equal(bits[1], Bit.One);
            Assert.Equal(bits[2], Bit.One);
            Assert.Equal(bits[3], Bit.One);

            Assert.Equal(bits[4], Bit.Zero);
            Assert.Equal(bits[5], Bit.One);
            Assert.Equal(bits[6], Bit.Zero);
            Assert.Equal(bits[7], Bit.Zero);

            Assert.Equal(bits[8], Bit.Zero);
            Assert.Equal(bits[9], Bit.One);
            Assert.Equal(bits[10], Bit.Zero);
            Assert.Equal(bits[11], Bit.One);

            Assert.Equal(bits[12], Bit.One);
        }

        [Fact]
        public void TestFromBytes()
        {
            var bytes = new byte[] { 0x00, 0x2E, 0x9A };
            var bits = BitArray.FromBytes(bytes, 13, 1 );
            Assert.Equal(13, bits.Length);

            byte[] expectedBytes = new byte[] { 0x2E, 0x1A };
            byte[] bitsAsBytes = bits.ToBytes();

            Assert.Equal(expectedBytes, bitsAsBytes);
        }

        [Fact]
        public void TestFromInt()
        {
            var bits = BitArray.FromInt(178464295);

            var expectedBits = BitArray.FromBinaryString("1110 0100 0110 0100 1100 0101 0101");

            Assert.Equal(expectedBits, bits);
        }

        [Fact]
        public void TestToInt32()
        {
            var bits = BitArray.FromBinaryString("1110 0100 0110 0100 1100 0101 0101");
            var expected = 178464295;

            var result = bits.ToInt32();
            Assert.Equal(expected, result);
        }

        [Fact]
        public void FromBigInteger()
        {
            var bytes = new byte[] { 0x27, 0x26, 0xA3, 0xAF, 0x70, 0x99 };

            var bitsMock = new Mock<BitSequence>() { CallBase = true };
            bitsMock.Setup(b => b.ToBytes()).Returns(bytes);

            var x = new BigInteger(0x9970AFA32727);
            var result = BitArray.FromBigInteger(x);

            var expected = BitArray.FromBinaryString("111001001110010011000101111101010000111010011001");

            Assert.Equal(expected, result);
        }

        [Fact]
        public void FromBigIntegerZero()
        {
            var expected = BitArray.FromBinaryString("0");
            var result = BitArray.FromBigInteger(BigInteger.Zero);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(129, 8, "10000001")]
        [InlineData(129, 9, "100000010")]
        [InlineData(129, 7, "1000000")]
        public void FromBigIntegerFixedLength(int i, int numberOfBits, string expectedString)
        {
            var input = new BigInteger(i);
            var result = BitArray.FromBigInteger(input, numberOfBits);

            var expected = BitArray.FromBinaryString(expectedString);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestOr()
        {
            var leftBits  = BitArray.FromBinaryString("01110100 01001");
            var rightBits = BitArray.FromBinaryString("01001000 00101");
            var expected  = BitArray.FromBinaryString("01111100 01101");

            var originalLeftBits = leftBits.Clone();

            var result = leftBits.Or(rightBits);
            Assert.Equal(expected.ToBytes(), result.ToBytes());
            Assert.Equal(originalLeftBits.ToBytes(), leftBits.ToBytes());

            var operatorResult = leftBits | rightBits;
            Assert.Equal(expected.ToBytes(), operatorResult.ToBytes());
            Assert.Equal(originalLeftBits.ToBytes(), leftBits.ToBytes());

            leftBits.InPlace.Or(rightBits);
            Assert.Equal(expected.ToBytes(), leftBits.ToBytes());
        }

        [Fact]
        public void TestXor()
        {
            var leftBits  = BitArray.FromBinaryString("01110100 01001");
            var rightBits = BitArray.FromBinaryString("01001000 00101");
            var expected  = BitArray.FromBinaryString("00111100 01100");

            var originalLeftBits = leftBits.Clone();

            var result = leftBits.Xor(rightBits);
            Assert.Equal(expected.ToBytes(), result.ToBytes());
            Assert.Equal(originalLeftBits.ToBytes(), leftBits.ToBytes());

            var operatorResult = leftBits ^ rightBits;
            Assert.Equal(expected.ToBytes(), operatorResult.ToBytes());
            Assert.Equal(originalLeftBits.ToBytes(), leftBits.ToBytes());

            leftBits.InPlace.Xor(rightBits);
            Assert.Equal(expected.ToBytes(), leftBits.ToBytes());
        }

        [Fact]
        public void TestAnd()
        {
            var leftBits  = BitArray.FromBinaryString("01110100 01001");
            var rightBits = BitArray.FromBinaryString("01001000 00101");
            var expected  = BitArray.FromBinaryString("01000000 00001");

            var originalLeftBits = leftBits.Clone();

            var result = leftBits.And(rightBits);
            Assert.Equal(expected.ToBytes(), result.ToBytes());
            Assert.Equal(originalLeftBits.ToBytes(), leftBits.ToBytes());

            var operatorResult = leftBits & rightBits;
            Assert.Equal(expected.ToBytes(), operatorResult.ToBytes());
            Assert.Equal(originalLeftBits.ToBytes(), leftBits.ToBytes());

            leftBits.InPlace.And(rightBits);
            Assert.Equal(expected.ToBytes(), leftBits.ToBytes());
        }

        [Fact]
        public void TestNot()
        {
            var leftBits  = BitArray.FromBinaryString("01110100 01001");
            var expected  = BitArray.FromBinaryString("10001011 10110");

            var originalLeftBits = leftBits.Clone();

            var result = leftBits.Not();
            Assert.Equal(expected.ToBytes(), result.ToBytes());
            Assert.Equal(originalLeftBits.ToBytes(), leftBits.ToBytes());

            var operatorResult = ~leftBits;
            Assert.Equal(expected.ToBytes(), operatorResult.ToBytes());
            Assert.Equal(originalLeftBits.ToBytes(), leftBits.ToBytes());

            leftBits.InPlace.Not();
            Assert.Equal(expected.ToBytes(), leftBits.ToBytes());
        }

        [Fact]
        public void TestToBytes()
        {
            var bits = BitArray.FromBytes(new byte[] { 0b10011011, 0b11100101 }, 11);
            var expectedBytes = new byte[] { 0b10011011, 0b00000101 };

            var bytes = bits.ToBytes();
            Assert.Equal(expectedBytes, bytes);
        }

        [Fact]
        public void TestToBytesAligned()
        {
            var bits = BitArray.FromBytes(new byte[] { 0b10011011, 0b11100101 }, 16);
            var expectedBytes = new byte[] { 0b10011011, 0b11100101 };

            var bytes = bits.ToBytes();
            Assert.Equal(expectedBytes, bytes);
        }

        [Fact]
        public void TestToBytesOneUnaligned()
        {
            var bits = BitArray.FromBytes(new byte[] { 0b10011011, 0b11100101 }, 9);
            var expectedBytes = new byte[] { 0b10011011, 0b00000001 };

            var bytes = bits.ToBytes();
            Assert.Equal(expectedBytes, bytes);
        }

        [Fact]
        public void TestEnumerator()
        {
            var bits  = BitArray.FromBinaryString("01110100 01001");
            Bit[] expectedBits = new Bit[] {
                Bit.Zero, Bit.One, Bit.One, Bit.One, Bit.Zero, Bit.One, Bit.Zero, Bit.Zero,
                Bit.Zero, Bit.One, Bit.Zero, Bit.Zero, Bit.One
            };
            foreach ((int i, Bit b) in bits.Enumerate())
            {
                Assert.True(expectedBits[i] == b, $"Expected {expectedBits[i]} but got {b} at position {i}.");
            }
        }
    }
}