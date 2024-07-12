// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using Xunit;
using System;
using System.Linq;

namespace CompactOT.DataStructures
{
    public class ObliviousTransferResultTests
    {
        [Fact]
        public void TestConstructor()
        {
            int numberOfInvocations = 3;
            int numberOfMessageBits = 82;
            var otResult = new ObliviousTransferResult(numberOfInvocations, numberOfMessageBits);

            Assert.Equal(numberOfInvocations, otResult.NumberOfInvocations);
            Assert.Equal(numberOfInvocations, otResult.Rows);
            Assert.Equal(numberOfMessageBits, otResult.NumberOfMessageBits);
            Assert.Equal(numberOfMessageBits, otResult.Cols);
        }

        [Fact]
        public void TestConstructorInvalidRows()
        {
            Assert.Throws<ArgumentException>(() => new ObliviousTransferResult(-1, 2));
        }

        [Fact]
        public void TestConstructorInvalidColumns()
        {
            Assert.Throws<ArgumentException>(() => new ObliviousTransferResult(1, -1));
        }

        [Fact]
        public void TestValuesConstructorAndGetInvocationResult()
        {
            int numberOfInvocations = 4;
            int numberOfMessageBits = 2;
            var otResult = new ObliviousTransferResult(numberOfInvocations, numberOfMessageBits);

            otResult.SetColumn(0, BitArray.FromBinaryString("0001"));
            otResult.SetColumn(1, BitArray.FromBinaryString("1011"));            

            var expectedResults = new BitArray[] {
                BitArray.FromBinaryString("01"),
                BitArray.FromBinaryString("00"),
                BitArray.FromBinaryString("01"),
                BitArray.FromBinaryString("11")
            };

            for (int i = 0; i < numberOfInvocations; ++i)
            {
                Assert.Equal(expectedResults[i], otResult.GetInvocationResult(i));
            }
        }

        [Fact]
        public void TestGetInvocationResultValueTooLarge()
        {
            int numberOfInvocations = 4;
            int numberOfMessageBits = 2;
            var otResult = new ObliviousTransferResult(numberOfInvocations, numberOfMessageBits);

            Assert.Throws<ArgumentOutOfRangeException>(() => otResult.GetInvocationResult(numberOfInvocations));
        }

        [Fact]
        public void TestGetInvocationResultIndexNegative()
        {
            int numberOfInvocations = 4;
            int numberOfMessageBits = 2;
            var otResult = new ObliviousTransferResult(numberOfInvocations, numberOfMessageBits);

            Assert.Throws<ArgumentOutOfRangeException>(() => otResult.GetInvocationResult(-1));
        }

        [Fact]
        public void TestAsBitSequences()
        {
            int numberOfInvocations = 4;
            int numberOfMessageBits = 2;
            var otResult = new ObliviousTransferResult(numberOfInvocations, numberOfMessageBits);

            otResult.SetColumn(0, BitArray.FromBinaryString("0001"));
            otResult.SetColumn(1, BitArray.FromBinaryString("1011"));

            var expectedResults = new BitArray[] {
                BitArray.FromBinaryString("01"),
                BitArray.FromBinaryString("00"),
                BitArray.FromBinaryString("01"),
                BitArray.FromBinaryString("11")
            };

            foreach (var indexAndResult in otResult.AsBitSequences().Select((result, i) => (i, result)))
            {
                Assert.Equal(expectedResults[indexAndResult.i], indexAndResult.result);
            }
        }

        [Fact]
        public void TestAsByteArrays()
        {
            int numberOfInvocations = 4;
            int numberOfMessageBits = 2;
            var otResult = new ObliviousTransferResult(numberOfInvocations, numberOfMessageBits);

            otResult.SetColumn(0, BitArray.FromBinaryString("0001"));
            otResult.SetColumn(1, BitArray.FromBinaryString("1011"));

            var expectedResults = new byte[][] {
                BitArray.FromBinaryString("01").AsByteEnumerable().ToArray(),
                BitArray.FromBinaryString("00").AsByteEnumerable().ToArray(),
                BitArray.FromBinaryString("01").AsByteEnumerable().ToArray(),
                BitArray.FromBinaryString("11").AsByteEnumerable().ToArray()
            };

            foreach (var indexAndResult in otResult.AsByteArrays().Select((result, i) => (i, result)))
            {
                Assert.Equal(expectedResults[indexAndResult.i], indexAndResult.result);
            }
        }

   }
}
