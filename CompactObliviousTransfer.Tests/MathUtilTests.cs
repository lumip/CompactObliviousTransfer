// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;

using Xunit;

namespace CompactOT
{
    public class MathUtilTests
    {

        [Theory]
        [InlineData(8, 4, 2)]
        [InlineData(29, 10, 3)]
        [InlineData(1, 3, 1)]
        public void TestDivideAndCeiling(int x, int y, int expected)
        {
            int result = MathUtil.DivideAndCeiling(x, y);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(2, 2)]
        [InlineData(8, 8)]
        [InlineData(7, 8)]
        public void TestNextPowerOfTwo(int x, int expected)
        {
            int result = MathUtil.NextPowerOfTwo(x);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestNextPowerOfTwoTooLargeInput()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MathUtil.NextPowerOfTwo(2147483647));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(128)]
        public void TestIsPowerOfTwoWithPowerOfTwo(int x)
        {
            Assert.True(MathUtil.IsPowerOfTwo(x));
        }

        [Theory]
        [InlineData(3)]
        [InlineData(12)]
        [InlineData(127)]
        [InlineData(129)]
        public void TestIsPowerOfTwoWithNoPowerOfTwo(int x)
        {
            Assert.False(MathUtil.IsPowerOfTwo(x));
        }

    }
}
