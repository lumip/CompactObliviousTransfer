// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections;
using System.Linq;
using System.Security.Cryptography;

using Moq;
using Xunit;

namespace CompactOT.Primitives
{
    public class RandomByteSequenceTests
    {

        [Fact]
        public void TestEnumerator()
        {
            var rngBuffer = new byte[] {
                1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15, 16,
               17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32
            };

            var randomNumberGeneratorMock = new Mock<RandomNumberGenerator>();
            randomNumberGeneratorMock
                .Setup(rng => rng.GetBytes(It.IsAny<byte[]>()))
                .Callback((byte[] b) =>
                    {
                        Array.Copy(rngBuffer, b, Math.Min(b.Length, rngBuffer.Length));
                    }
                );

            var sequence = new RandomByteSequence(randomNumberGeneratorMock.Object);
            var enumerator = sequence.Enumerator;

            for (int i = 0; i < 2; i++)
            {
                foreach (byte expected in rngBuffer)
                {
                    enumerator.MoveNext();
                    byte value = enumerator.Current;
                    Assert.Equal(expected, value);

                    value = enumerator.Current;
                    Assert.Equal(expected, value);
                }
            }

            byte? nonGenericValue = ((IEnumerator)enumerator).Current as byte?;
            Assert.Equal(rngBuffer[^1], nonGenericValue);

            randomNumberGeneratorMock.Verify(rng => rng.GetBytes(It.IsAny<byte[]>()), Times.Exactly(2));
        }

        [Fact]
        public void TestEnumeratorReset()
        {
            var randomNumberGeneratorMock = new Mock<RandomNumberGenerator>();
            var sequence = new RandomByteSequence(randomNumberGeneratorMock.Object);
            var enumerator = sequence.Enumerator;

            Assert.Throws<NotSupportedException>(enumerator.Reset);
        }

        [Fact]
        public void TestGetBits()
        {
            var rngBuffer = new byte[] {
                0b11001101, 0b11110010, 0b10101010, 0b11111101
            };

            var sequence = new RandomByteSequence(rngBuffer.AsEnumerable());

            var expected = DataStructures.BitArray.Empty;
            var actual = sequence.GetBits(0);
            Assert.Equal(expected, actual);

            expected = DataStructures.BitArray.FromBinaryString("10");
            actual = sequence.GetBits(2);
            Assert.Equal(expected, actual);

            expected = DataStructures.BitArray.FromBinaryString("010");
            actual = sequence.GetBits(3);
            Assert.Equal(expected, actual);

            expected = DataStructures.BitArray.FromBinaryString("0101010110");
            actual = sequence.GetBits(10);
            Assert.Equal(expected, actual);
        }

    }
}
