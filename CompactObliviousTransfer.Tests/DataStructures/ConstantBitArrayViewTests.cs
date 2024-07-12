// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using Xunit;

namespace CompactOT.DataStructures
{
    public class ConstantBitArrayViewTests
    {
        [Fact]
        public void TestMakeOnes()
        {
            int length = 17;
            var bits = ConstantBitArrayView.MakeOnes(length);

            Assert.Equal(length, bits.Length);
            foreach (Bit b in bits)
            {
                Assert.Equal(Bit.One, b);
            }
        }

        [Fact]
        public void TestMakeZeros()
        {
            int length = 15;
            var bits = ConstantBitArrayView.MakeZeros(length);

            Assert.Equal(length, bits.Length);
            foreach (Bit b in bits)
            {
                Assert.Equal(Bit.Zero, b);
            }
        }

    }
}