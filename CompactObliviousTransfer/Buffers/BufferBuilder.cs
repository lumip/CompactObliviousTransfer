// SPDX-FileCopyrightText: 2018 Jonas Nagy-Kuhlen <jonas.nagy-kuhlen@rwth-aachen.de>, 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: MIT
// Adopted from CompactMPC: https://github.com/jnagykuhlen/CompactMPC

using CompactOT.DataStructures;

namespace CompactOT.Buffers
{
    public class BufferBuilder
    {
        private MessageComposer _composer;

        private BufferBuilder()
        {
            _composer = new MessageComposer();
        }

        public static BufferBuilder Empty => new BufferBuilder();

        public static BufferBuilder From(byte[] buffer)
        {
            return new BufferBuilder().With(buffer);
        }

        public BufferBuilder With(byte[] buffer)
        {
            _composer.Write(buffer);
            return this;
        }

        public BufferBuilder With(int value)
        {
            _composer.Write(value);
            return this;
        }

        public BufferBuilder With(BitSequence bits)
        {
            _composer.Write(bits);
            return this;
        }

        public BufferBuilder With(BitMatrix matrix)
        {
            _composer.Write(matrix);
            return this;
        }

        public byte[] Create()
        {
            return _composer.Compose();
        }
    }
}
