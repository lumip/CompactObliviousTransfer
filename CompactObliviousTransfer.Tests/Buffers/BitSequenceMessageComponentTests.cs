// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using Xunit;

namespace CompactOT.Buffers.Internal
{
    public class BitSequenceMessageComponentTests
    {

        [Fact]
        public void TestWriteToBuffer()
        {
            var value = DataStructures.BitArray.FromBinaryString("11000110 10100101 11");
            var component = new BitSequenceMessageComponent(value);

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
            var expectedValue = DataStructures.BitArray.FromBinaryString("11000110 10100101 11");
            byte[] messageBuffer = new byte[] { 1, 2, 0x63, 0xa5, 0x03, 6, 7, 8, 9, 10 };
            int offset = 2;

            var value = BitSequenceMessageComponent.ReadFromBuffer(messageBuffer, ref offset, expectedValue.Length);

            int expectedOffset = 2 + 3;
            Assert.Equal(expectedOffset, offset);
            Assert.Equal(expectedValue, value);
        }

        [Fact]
        public void TestLength()
        {
            var value = DataStructures.BitArray.FromBinaryString("11000110 10100101 11");
            var component = new BitSequenceMessageComponent(value);

            Assert.Equal(3, component.Length);
        }

    }

}
