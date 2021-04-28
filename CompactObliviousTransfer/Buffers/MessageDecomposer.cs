// SPDX-FileCopyrightText: 2018 Jonas Nagy-Kuhlen <jonas.nagy-kuhlen@rwth-aachen.de>
// SPDX-License-Identifier: MIT
// Adopted from CompactMPC: https://github.com/jnagykuhlen/CompactMPC

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CompactOT.Buffers.Internal;
using CompactOT.DataStructures;

namespace CompactOT.Buffers
{
    public class MessageDecomposer
    {
        private byte[] _messageBuffer;
        private int _offset;

        public MessageDecomposer(byte[] messageBuffer)
        {
            _messageBuffer = messageBuffer;
            _offset = 0;
        }

        public byte[] ReadBuffer(int length)
        {
            return BufferMessageComponent.ReadFromBuffer(_messageBuffer, ref _offset, length);
        }

        public int ReadInt()
        {
            return IntMessageComponent.ReadFromBuffer(_messageBuffer, ref _offset);
        }

        public BitArrayBase ReadBitArray(int numberOfElements)
        {
            return BitArrayMessageComponent.ReadFromBuffer(_messageBuffer, ref _offset, numberOfElements);
        }

        public BitMatrix ReadBitMatrix(int numberOfRows, int numberOfColumns)
        {
            return BitMatrixMessageComponent.ReadFromBuffer(_messageBuffer, ref _offset, numberOfRows, numberOfColumns);
        }

        public int Length
        {
            get
            {
                return _messageBuffer.Length;
            }
        }
    }
}
