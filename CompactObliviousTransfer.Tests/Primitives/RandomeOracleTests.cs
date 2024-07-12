// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections;
using System.Linq;
using Moq;
using Xunit;

namespace CompactOT.Primitives
{
    public class RandomOracleTests
    {

        [Fact]
        public void TestInvokeWithEnumerable()
        {
            var expectedBytes = new byte[] { 0, 1, 2, 3, 4 };
            var expectedSequence = new RandomByteSequence(expectedBytes);

            var oracleMock = new Mock<RandomOracle>() { CallBase = true };
            oracleMock.Setup(o => o.Invoke(It.IsAny<byte[]>())).Returns(expectedSequence);
            var oracle = oracleMock.Object;

            var query = new byte[] { 0, 1 };
            var resultSequence = oracle.Invoke(query.Reverse());

            var expectedBits = DataStructures.BitArray.FromBytes(expectedBytes, 5*8);
            var resultBits = resultSequence.GetBits(expectedBits.Length);

            Assert.Equal(expectedBits, resultBits);

            var expectedQuery = new byte[] { 1, 0 };

            oracleMock.Verify(o => o.Invoke(It.Is<byte[]>(q => q.SequenceEqual(expectedQuery))), Times.Once);
        }

        [Fact]
        public void TestMaskWithBytes()
        {
            var messageBytes = new byte[] { 0b0110 };
        
            var oracleBytes = new byte[] { 0b1100 };
            var oracleSequence = new RandomByteSequence(oracleBytes);

            var oracleMock = new Mock<RandomOracle>() { CallBase = true };
            oracleMock.Setup(o => o.Invoke(It.IsAny<byte[]>())).Returns(oracleSequence);
            var oracle = oracleMock.Object;

            var query = new byte[] { 0, 1 };

            var expectedBytes = new byte[] { 0b1010 };

            var resultBytes = oracle.Mask(messageBytes, query);

            Assert.Equal(expectedBytes, resultBytes);

            oracleMock.Verify(o => o.Invoke(It.Is<byte[]>(q => q.SequenceEqual(query))), Times.Once);
        }

        [Fact]
        public void TestMaskNotEnoughRandomBytes()
        {
            var messageBytes = new byte[] { 0b0110, 0b1001 };
        
            var oracleBytes = new byte[] { 0b1100 };
            var oracleSequence = new RandomByteSequence(oracleBytes);

            var oracleMock = new Mock<RandomOracle>() { CallBase = true };
            oracleMock.Setup(o => o.Invoke(It.IsAny<byte[]>())).Returns(oracleSequence);
            var oracle = oracleMock.Object;

            var query = new byte[] { 0, 1 };

            var expectedBytes = new byte[] { 0b1010 };

            Assert.Throws<ArgumentException>(() => oracle.Mask(messageBytes, query));
        }

        [Fact]
        public void TestMaskWithBitSequences()
        {
            var messageBytes = new byte[] { 0b0110 };
            var messageBits = DataStructures.BitArray.FromBytes(messageBytes, 4);
        
            var oracleBytes = new byte[] { 0b1100 };
            var oracleSequence = new RandomByteSequence(oracleBytes);

            var oracleMock = new Mock<RandomOracle>() { CallBase = true };
            oracleMock.Setup(o => o.Invoke(It.IsAny<byte[]>())).Returns(oracleSequence);
            var oracle = oracleMock.Object;

            var query = new byte[] { 0, 1, 2 };
            var queryBits = DataStructures.BitArray.FromBytes(query, 16);
            var expectedQuery = new byte[] { 0, 1 };

            var expectedBytes = new byte[] { 0b1010 };
            var expectedBits = DataStructures.BitArray.FromBytes(expectedBytes, 4);

            var resultBits = oracle.Mask(messageBits, queryBits);

            Assert.Equal(expectedBits, resultBits);

            oracleMock.Verify(o => o.Invoke(It.Is<byte[]>(q => q.SequenceEqual(expectedQuery))), Times.Once);
        }

        [Fact]
        public void TestMaskWithBitSequenceAndByteEnumerableQuery()
        {
            var messageBytes = new byte[] { 0b0110 };
            var messageBits = DataStructures.BitArray.FromBytes(messageBytes, 4);
        
            var oracleBytes = new byte[] { 0b1100 };
            var oracleSequence = new RandomByteSequence(oracleBytes);

            var oracleMock = new Mock<RandomOracle>() { CallBase = true };
            oracleMock.Setup(o => o.Invoke(It.IsAny<byte[]>())).Returns(oracleSequence);
            var oracle = oracleMock.Object;

            var query = new byte[] { 0, 1, 2 };

            var expectedBytes = new byte[] { 0b1010 };
            var expectedBits = DataStructures.BitArray.FromBytes(expectedBytes, 4);

            var resultBits = oracle.Mask(messageBits, query);

            Assert.Equal(expectedBits, resultBits);

            oracleMock.Verify(o => o.Invoke(It.Is<byte[]>(q => q.SequenceEqual(query))), Times.Once);
        }

    }

}
