// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using Xunit;

namespace CompactOT.Buffers.Internal
{
    public class BufferBuilderTests
    {

        [Fact]
        public void TestBufferBuilderFromEmpty()
        {
            int intValue = 0x0f1ecc02;
            byte[] bufferValue = new byte[] { 0xaa, 0xbb, 0xcc };
            DataStructures.BitArray bitArrayValue = DataStructures.BitArray.FromBinaryString("110001");
            DataStructures.BitArray bitMatrixBits = DataStructures.BitArray.FromBinaryString("101 010 111");
            DataStructures.BitMatrix bitMatrixValue = new DataStructures.BitMatrix(3, 3, bitMatrixBits);

            var bufferBuilder = BufferBuilder.Empty
                .With(intValue)
                .With(bitArrayValue)
                .With(bitMatrixValue)
                .With(bufferValue);

            byte[] buffer = bufferBuilder.Create();
            byte[] expectedBuffer = new byte[] {
                0x02, 0xcc, 0x1e, 0xf,
                0x23,
                0xd5, 0x01,
                0xaa, 0xbb, 0xcc
            };

            Assert.Equal(expectedBuffer, buffer);
        }

        [Fact]
        public void TestBufferBuildFromBuffer()
        {
            byte[] bufferValue = new byte[] { 0xaa, 0xbb, 0xcc };
            var bufferBuilder = BufferBuilder.From(bufferValue);

            byte[] buffer = bufferBuilder.Create();
            Assert.Equal(bufferValue, buffer);
        }

    }

}
