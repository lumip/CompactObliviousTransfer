// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace CompactOT.DataStructures
{
    public class ByteEnumerableOperationsTests
    {
        [Fact]
        public void TestAnd()
        {
            var leftArray = new byte[] { 0x36, 0xC9 };
            var rightArray = new byte[] { 0x6C, 0x93 };

            var expectedArray = new byte[] { 0x24, 0x81 };

            IEnumerable<byte> resultEnumerable = ByteEnumerableOperations.And(
                leftArray.AsEnumerable(), rightArray.AsEnumerable()
            );

            Assert.IsNotType<byte[]>(resultEnumerable);
            Assert.Equal(expectedArray, resultEnumerable);

            byte[] resultArray = ByteEnumerableOperations.And(
                leftArray, rightArray
            );
            Assert.IsType<byte[]>(resultArray);
            Assert.Equal(expectedArray, resultArray);
        }

        [Fact]
        public void TestInPlaceAnd()
        {
            var leftArray = new byte[] { 0x36, 0xC9 };
            var rightArray = new byte[] { 0x6C, 0x93 };

            var expectedArray = new byte[] { 0x24, 0x81 };

            ByteEnumerableOperations.InPlaceAnd(
                leftArray, rightArray.AsEnumerable()
            );

            Assert.Equal(expectedArray, leftArray);
        }

        [Fact]
        public void TestOr()
        {
            var leftArray = new byte[] { 0x36, 0xC9 };
            var rightArray = new byte[] { 0x6C, 0x93 };

            var expectedArray = new byte[] { 0x7E, 0xDB };

            IEnumerable<byte> resultEnumerable = ByteEnumerableOperations.Or(
                leftArray.AsEnumerable(), rightArray.AsEnumerable()
            );

            Assert.IsNotType<byte[]>(resultEnumerable);
            Assert.Equal(expectedArray, resultEnumerable);

            byte[] resultArray = ByteEnumerableOperations.Or(
                leftArray, rightArray
            );
            Assert.IsType<byte[]>(resultArray);
            Assert.Equal(expectedArray, resultArray);
        }

        [Fact]
        public void TestInPlaceOr()
        {
            var leftArray = new byte[] { 0x36, 0xC9 };
            var rightArray = new byte[] { 0x6C, 0x93 };

            var expectedArray = new byte[] { 0x7E, 0xDB };

            ByteEnumerableOperations.InPlaceOr(
                leftArray, rightArray.AsEnumerable()
            );
            Assert.Equal(expectedArray, leftArray);
        }

        [Fact]
        public void TestXor()
        {
            var leftArray = new byte[] { 0x36, 0xC9 };
            var rightArray = new byte[] { 0x6C, 0x93 };

            var expectedArray = new byte[] { 0x5A, 0x5A };

            IEnumerable<byte> resultEnumerable = ByteEnumerableOperations.Xor(
                leftArray.AsEnumerable(), rightArray.AsEnumerable()
            );

            Assert.IsNotType<byte[]>(resultEnumerable);
            Assert.Equal(expectedArray, resultEnumerable);

            byte[] resultArray = ByteEnumerableOperations.Xor(
                leftArray, rightArray
            );
            Assert.IsType<byte[]>(resultArray);
            Assert.Equal(expectedArray, resultArray);
        }


        [Fact]
        public void TestInPlaceXor()
        {
            var leftArray = new byte[] { 0x36, 0xC9 };
            var rightArray = new byte[] { 0x6C, 0x93 };

            var expectedArray = new byte[] { 0x5A, 0x5A };

            ByteEnumerableOperations.InPlaceXor(
                leftArray, rightArray.AsEnumerable()
            );
            Assert.Equal(expectedArray, leftArray);
        }

        [Fact]
        public void TestNot()
        {
            var array = new byte[] { 0x36, 0xC9 };

            var expectedArray = new byte[] { 0xC9, 0x36 };

            IEnumerable<byte> resultEnumerable = ByteEnumerableOperations.Not(
                array.AsEnumerable()
            );

            Assert.IsNotType<byte[]>(resultEnumerable);
            Assert.Equal(expectedArray, resultEnumerable);

            byte[] resultArray = ByteEnumerableOperations.Not(
                array
            );
            Assert.IsType<byte[]>(resultArray);
            Assert.Equal(expectedArray, resultArray);
        }

        [Fact]
        public void TestInPlaceNot()
        {
            var array = new byte[] { 0x36, 0xC9 };

            var expectedArray = new byte[] { 0xC9, 0x36 };

            ByteEnumerableOperations.InPlaceNot(
                array
            );
            Assert.Equal(expectedArray, array);
        }

    }

}
