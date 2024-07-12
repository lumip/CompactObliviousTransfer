// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using Xunit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CompactOT.DataStructures
{
    public class BitToByteEnumerableTests
    {

        [Fact]
        public void TestEnumerable()
        {
            Bit[] bits = new Bit[] {
                Bit.Zero,  Bit.One, Bit.Zero, Bit.Zero,  Bit.One, Bit.Zero, Bit.Zero, Bit.Zero,
                 Bit.One,  Bit.One, Bit.Zero, Bit.Zero, Bit.Zero
            };
            byte[] expectedBytes = new byte[] { 0x12, 0x03 };

            var enumerable = new BitToByteEnumerable(bits);
            Assert.Equal(2, enumerable.Count());

            var bytes = enumerable.ToArray();
            Assert.Equal(expectedBytes, bytes);
        }

        [Fact]
        public void TestGetEnumeratorGeneric()
        {
            Bit[] bits = new Bit[] {
                Bit.Zero,  Bit.One, Bit.Zero, Bit.Zero,  Bit.One, Bit.Zero, Bit.Zero, Bit.Zero,
                 Bit.One,  Bit.One, Bit.Zero, Bit.Zero, Bit.Zero
            };

            var enumerable = new BitToByteEnumerable(bits);
            var enumerator = enumerable.GetEnumerator();

            Assert.IsType<BitToByteEnumerator>(enumerator);
        }

        [Fact]
        public void TestGetEnumeratorNonGeneric()
        {
            Bit[] bits = new Bit[] {
                Bit.Zero,  Bit.One, Bit.Zero, Bit.Zero,  Bit.One, Bit.Zero, Bit.Zero, Bit.Zero,
                 Bit.One,  Bit.One, Bit.Zero, Bit.Zero, Bit.Zero
            };

            var enumerable = new BitToByteEnumerable(bits);
            var enumerator = ((IEnumerable)enumerable).GetEnumerator();

            Assert.IsType<BitToByteEnumerator>(enumerator);
        }

    }
}