// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Linq;

using Xunit;

namespace CompactOT.DataStructures
{
    public class ByteToBitEnumerableTests
    {

        [Fact]
        public void TestEnumerable()
        {
            byte[] bytes = new byte[] { 0x12, 0x03 };
            Bit[] expectedBits = new Bit[] {
                Bit.Zero,  Bit.One, Bit.Zero, Bit.Zero,  Bit.One, Bit.Zero, Bit.Zero, Bit.Zero,
                 Bit.One,  Bit.One, Bit.Zero, Bit.Zero, Bit.Zero
            };

            var enumerable = new ByteToBitEnumerable(bytes, expectedBits.Length);
            Assert.Equal(expectedBits.Length, enumerable.Count());

            var bits = enumerable.ToArray();
            Assert.Equal(expectedBits, bits);
        }

        [Fact]
        public void TestConstructorRejectsNegativeNumberOfBits()
        {
            byte[] bytes = new byte[] { 0x12, 0x03 };
            Assert.Throws<ArgumentOutOfRangeException>(() => new ByteToBitEnumerable(bytes, -1));
        }

        [Fact]
        public void TestGetEnumeratorGeneric()
        {
            byte[] bytes = new byte[] { 0x12, 0x03 };

            var enumerable = new ByteToBitEnumerable(bytes, 13);
            var enumerator = enumerable.GetEnumerator();

            Assert.IsType<ByteToBitEnumerator>(enumerator);
        }

    }
}
