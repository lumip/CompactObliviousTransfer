﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompactOT.Buffers.Internal
{
    public class BufferMessageComponent : IMessageComponent
    {
        private byte[] _buffer;

        public BufferMessageComponent(byte[] buffer)
        {
            _buffer = buffer;
        }

        public void WriteToBuffer(byte[] messageBuffer, ref int offset)
        {
            Buffer.BlockCopy(_buffer, 0, messageBuffer, offset, _buffer.Length);
            offset += _buffer.Length;
        }

        public static byte[] ReadFromBuffer(byte[] messageBuffer, ref int offset, int length)
        {
            byte[] buffer = new byte[length];
            Buffer.BlockCopy(messageBuffer, offset, buffer, 0, length);
            offset += length;
            return buffer;
        }

        public int Length
        {
            get
            {
                return _buffer.Length;
            }
        }
    }
}
