// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Xunit;
using Moq;

namespace CompactOT
{

    public class RandomByteSequenceTests
    {
        [Fact]
        public void TestRepeatedInvocation()
        {
            byte[] randomBytes = new byte[] { 0x52, 0xf1 };
            var randomSequence = new RandomByteSequence(randomBytes);
            DataStructures.BitSequence bits1 = randomSequence.GetBits(7);
            DataStructures.BitSequence bits2 = randomSequence.GetBits(7);
            Assert.NotEqual(bits1.ToBytes(), bits2.ToBytes());
        }
    }

    public class RandomNumberGeneratorExtensionsTests
    {
        [Fact]
        public void TestGetInt32Array()
        {
            int amount = 4;
            int toExclusive = 5;

            var rngMock = new Mock<RandomNumberGenerator>();
            var rngBuffers = new Queue<byte[]>(new byte[][] { new byte[] { 0x2b, 0xbf }, new byte[] { 0xd6, 0x7a } });
            // 0xbf2b -> 3, 5, 4, 7, 3,
            // 0x7ad6 -> 6, 2, 3, 5, 7
            var expected = new int[] { 3, 4, 3, 2 };

            rngMock.Setup(r => r.GetBytes(It.IsAny<byte[]>())).Callback((byte[] b) => {
                var buffer = rngBuffers.Dequeue();
                Array.Copy(buffer, b, Math.Min(b.Length, buffer.Length));
            });

            var result = rngMock.Object.GetInt32Array(toExclusive, amount);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestGetInt32ArrayEndingZero()
        {
            int amount = 3;
            int toExclusive = 5;

            var rngMock = new Mock<RandomNumberGenerator>();
            var rngBuffer = new byte[] { 0x57, 0x03 };
            // 0x0357 -> 7, 2, 5, 1, 0
            var expected = new int[] { 2, 1, 0 };
            rngMock.Setup(r => r.GetBytes(It.IsAny<byte[]>())).Callback((byte[] b) => {
                Array.Copy(rngBuffer, b, Math.Min(b.Length, rngBuffer.Length));
            });

            var result = rngMock.Object.GetInt32Array(toExclusive, amount);
            Assert.Equal(expected, result);
        }
    }

}