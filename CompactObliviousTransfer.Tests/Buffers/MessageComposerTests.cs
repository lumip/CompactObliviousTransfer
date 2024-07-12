// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using Xunit;

namespace CompactOT.Buffers.Internal
{
    public class MessageComposerTests
    {

        [Fact]
        public void TestMessageComposer()
        {
            int intValue = 0x0f1ecc02;
            byte[] bufferValue = new byte[] { 0xaa, 0xbb, 0xcc };
            DataStructures.BitArray bitArrayValue = DataStructures.BitArray.FromBinaryString("110001");
            DataStructures.BitArray bitMatrixBits = DataStructures.BitArray.FromBinaryString("101 010 111");
            DataStructures.BitMatrix bitMatrixValue = new DataStructures.BitMatrix(3, 3, bitMatrixBits);

            var composer = new MessageComposer();
            composer.Write(intValue);
            composer.Write(bitArrayValue);
            composer.Write(bitMatrixValue);
            composer.Write(bufferValue);

            int expectedLength = 4 + 1 + 2 + 3;
            Assert.Equal(expectedLength, composer.Length);

            byte[] buffer = composer.Compose();
            byte[] expectedBuffer = new byte[] {
                0x02, 0xcc, 0x1e, 0xf,
                0x23,
                0xd5, 0x01,
                0xaa, 0xbb, 0xcc
            };

            Assert.Equal(expectedBuffer, buffer);
        }

    }

}
