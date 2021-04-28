// SPDX-FileCopyrightText: 2018 Jonas Nagy-Kuhlen <jonas.nagy-kuhlen@rwth-aachen.de>
// SPDX-License-Identifier: MIT
// Adopted from CompactMPC: https://github.com/jnagykuhlen/CompactMPC

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public BufferBuilder With(BitArrayBase bits)
        {
            _composer.Write(bits);
            return this;
        }

        public byte[] Create()
        {
            return _composer.Compose();
        }
    }
}
