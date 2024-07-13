// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using CompactOT.DataStructures;

using Xunit;

namespace CompactOT.Buffers.Internal
{
    public class BitMatrixMessageComponentTests
    {

        [Fact]
        public void TestWriteToBuffer()
        {
            var flatValue = DataStructures.BitArray.FromBinaryString("11000110 10100101 11");
            var value = new BitMatrix(6, 3, flatValue);
            var component = new BitMatrixMessageComponent(value);

            byte[] messageBuffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            int offset = 2;
            component.WriteToBuffer(
                messageBuffer, ref offset
            );

            int expectedOffset = 2 + 3;
            byte[] expectedMessageBuffer = new byte[] { 1, 2, 0x63, 0xa5, 0x03, 6, 7, 8, 9, 10 };
            Assert.Equal(expectedOffset, offset);
            Assert.Equal(expectedMessageBuffer, messageBuffer);
        }

        [Fact]
        public void TestReadFromBuffer()
        {
            var expectedFlatValue = DataStructures.BitArray.FromBinaryString("11000110 10100101 11");
            var expectedValue = new BitMatrix(6, 3, expectedFlatValue);

            byte[] messageBuffer = new byte[] { 1, 2, 0x63, 0xa5, 0x03, 6, 7, 8, 9, 10 };
            int offset = 2;

            var value = BitMatrixMessageComponent.ReadFromBuffer(messageBuffer, ref offset, expectedValue.Rows, expectedValue.Cols);

            int expectedOffset = 2 + 3;
            Assert.Equal(expectedOffset, offset);
            Assert.Equal(expectedValue, value);
        }

        [Fact]
        public void TestLength()
        {
            var flatValue = DataStructures.BitArray.FromBinaryString("11000110 10100101 11");
            var value = new BitMatrix(6, 3, flatValue);
            var component = new BitMatrixMessageComponent(value);

            Assert.Equal(3, component.Length);
        }

    }

}
