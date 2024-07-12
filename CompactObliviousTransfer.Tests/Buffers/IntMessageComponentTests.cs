// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using Xunit;

namespace CompactOT.Buffers.Internal
{
    public class IntMessageComponentTests
    {

        [Fact]
        public void TestWriteToBuffer()
        {
            int value = 0x0f1ecc02;
            var component = new IntMessageComponent(value);

            byte[] buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            int offset = 2;
            component.WriteToBuffer(
                buffer, ref offset
            );

            int expectedOffset = 2 + 4;
            byte[] expectedBuffer = new byte[] { 1, 2, 0x02, 0xcc, 0x1e, 0xf, 7, 8, 9, 10 };
            Assert.Equal(expectedOffset, offset);
            Assert.Equal(expectedBuffer, buffer);
        }

        [Fact]
        public void TestReadFromBuffer()
        {
            int expectedValue = 0x0f1ecc02;
            byte[] buffer = new byte[] { 1, 2, 0x02, 0xcc, 0x1e, 0xf, 7, 8, 9, 10 };
            int offset = 2;

            int value = IntMessageComponent.ReadFromBuffer(buffer, ref offset);

            int expectedOffset = 2 + 4;
            Assert.Equal(expectedOffset, offset);
            Assert.Equal(expectedValue, value);
        }

        [Fact]
        public void TestLength()
        {
            int value = 17;
            var component = new IntMessageComponent(value);

            Assert.Equal(sizeof(int), component.Length);
        }

    }

}
