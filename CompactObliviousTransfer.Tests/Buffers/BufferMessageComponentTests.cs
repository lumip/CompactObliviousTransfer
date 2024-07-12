// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using Xunit;

namespace CompactOT.Buffers.Internal
{
    public class BufferMessageComponentTests
    {

        [Fact]
        public void TestWriteToBuffer()
        {
            byte[] valueBuffer = new byte[] { 0x02, 0xcc, 0x1e, 0xf };
            var component = new BufferMessageComponent(valueBuffer);

            byte[] messageBuffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            int offset = 2;
            component.WriteToBuffer(
                messageBuffer, ref offset
            );

            int expectedOffset = 2 + 4;
            byte[] expectedMessageBuffer = new byte[] { 1, 2, 0x02, 0xcc, 0x1e, 0xf, 7, 8, 9, 10 };
            Assert.Equal(expectedOffset, offset);
            Assert.Equal(expectedMessageBuffer, messageBuffer);
        }

        [Fact]
        public void TestReadFromBuffer()
        {
            byte[] expectedValueBuffer = new byte[] { 0x02, 0xcc, 0x1e, 0xf };
            byte[] messageBuffer = new byte[] { 1, 2, 0x02, 0xcc, 0x1e, 0xf, 7, 8, 9, 10 };
            int offset = 2;

            byte[] valueBuffer = BufferMessageComponent.ReadFromBuffer(messageBuffer, ref offset, expectedValueBuffer.Length);

            int expectedOffset = 2 + 4;
            Assert.Equal(expectedOffset, offset);
            Assert.Equal(expectedValueBuffer, valueBuffer);
        }

        [Fact]
        public void TestLength()
        {
            int expectedLength = 63;
            byte[] valueBuffer = new byte[expectedLength];
            var component = new BufferMessageComponent(valueBuffer);

            Assert.Equal(expectedLength, component.Length);
        }

    }

}
