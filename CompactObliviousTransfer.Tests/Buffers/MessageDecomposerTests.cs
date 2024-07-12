// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using Xunit;

namespace CompactOT.Buffers.Internal
{
    public class MessageDecomposerTests
    {

        [Fact]
        public void TestMessageDecomposer()
        {
            byte[] messageBuffer = new byte[] {
                0x02, 0xcc, 0x1e, 0xf,
                0x23,
                0xd5, 0x01,
                0xaa, 0xbb, 0xcc
            };

            int expectedInt = 0x0f1ecc02;
            byte[] expectedBuffer = new byte[] { 0xaa, 0xbb, 0xcc };
            DataStructures.BitArray expectedBitArray = DataStructures.BitArray.FromBinaryString("110001");
            DataStructures.BitArray bitMatrixBits = DataStructures.BitArray.FromBinaryString("101 010 111");
            DataStructures.BitMatrix expectedBitMatrix = new DataStructures.BitMatrix(3, 3, bitMatrixBits);

            var decomposer = new MessageDecomposer(messageBuffer);
            Assert.Equal(messageBuffer.Length, decomposer.Length);

            int intValue = decomposer.ReadInt();
            var bitArrayValue = decomposer.ReadBitArray(expectedBitArray.Length);
            var bitMatrixValue = decomposer.ReadBitMatrix(expectedBitMatrix.Rows, expectedBitMatrix.Cols);
            byte[] bufferValue = decomposer.ReadBuffer(expectedBuffer.Length);

            Assert.Equal(expectedInt, intValue);
            Assert.Equal(expectedBitArray, bitArrayValue);
            Assert.Equal(expectedBitMatrix, bitMatrixValue);
            Assert.Equal(expectedBuffer, bufferValue);
        }

    }

}
