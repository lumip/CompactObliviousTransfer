// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using Xunit;
using Moq;

namespace CompactOT.DataStructures
{
    public class BitTests
    {

        [Fact]
        public void TestByteConstructor()
        {
            Assert.True(new Bit(1).Value);
            Assert.False(new Bit(0).Value);
        }

        [Fact]
        public void TestBoolConstructor()
        {
            Assert.True(new Bit(true).Value);
            Assert.False(new Bit(false).Value);
        }

        [Fact]
        public void TestZeroValue()
        {
            var bit = Bit.Zero;

            Assert.False(bit.Value);
            Assert.Equal(0, bit.GetHashCode());
        }

        [Fact]
        public void TestOneValue()
        {
            var bit = Bit.One;

            Assert.True(bit.Value);
            Assert.Equal(1, bit.GetHashCode());
        }

        [Fact]
        public void TestEquals()
        {
            var bit = Bit.One;
            var equalBit = Bit.One;
            var unequalBit = Bit.Zero;

            Assert.True(bit.Equals(equalBit));
            Assert.False(bit.Equals(unequalBit));
            Assert.False(bit.Equals(new object()));
            Assert.False(bit.Equals(null));
        }

        [Fact]
        public void TestEqualityOperators()
        {
            var bit = Bit.One;
            var equalBit = Bit.One;

            Assert.True(bit == equalBit);
            Assert.False(bit != equalBit);

            var unequalBit = Bit.Zero;
            Assert.False(bit == unequalBit);
            Assert.True(bit != unequalBit);
        }

        [Fact]
        public void TestOrOperator()
        {
            Assert.Equal(Bit.Zero, Bit.Zero | Bit.Zero);
            Assert.Equal(Bit.One, Bit.Zero | Bit.One);
            Assert.Equal(Bit.One, Bit.One | Bit.Zero);
            Assert.Equal(Bit.One, Bit.One | Bit.One);
        }

        [Fact]
        public void TestXorOperator()
        {
            Assert.Equal(Bit.Zero, Bit.Zero ^ Bit.Zero);
            Assert.Equal(Bit.One, Bit.Zero ^ Bit.One);
            Assert.Equal(Bit.One, Bit.One ^ Bit.Zero);
            Assert.Equal(Bit.Zero, Bit.One ^ Bit.One);
        }

        [Fact]
        public void TestAndOperator()
        {
            Assert.Equal(Bit.Zero, Bit.Zero & Bit.Zero);
            Assert.Equal(Bit.Zero, Bit.Zero & Bit.One);
            Assert.Equal(Bit.Zero, Bit.One & Bit.Zero);
            Assert.Equal(Bit.One, Bit.One & Bit.One);
        }

        [Fact]
        public void TestNegateOperator()
        {
            Assert.Equal(Bit.One, ~Bit.Zero);
            Assert.Equal(Bit.Zero, ~Bit.One);
        }

        [Fact]
        public void TestCastToByte()
        {
            Assert.Equal(0, (byte)Bit.Zero);
            Assert.Equal(1, (byte)Bit.One);
        }

        [Fact]
        public void TestCastToBool()
        {
            Assert.True((bool)Bit.One);
            Assert.False((bool)Bit.Zero);
        }

        [Fact]
        public void TestCastFromByte()
        {
            Assert.Equal(Bit.Zero, (Bit)0);
            Assert.Equal(Bit.One, (Bit)1);
        }

        [Fact]
        public void TestCastFromBool()
        {
            Assert.Equal(Bit.One, (Bit)true);
            Assert.Equal(Bit.Zero, (Bit)false);
        }

        [Fact]
        public void TestToString()
        {
            Assert.Equal("Bit(0)", Bit.Zero.ToString());
            Assert.Equal("Bit(1)", Bit.One.ToString());
        }

    }
}