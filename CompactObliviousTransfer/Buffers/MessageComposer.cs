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
    public class MessageComposer
    {
        private const int DefaultExpectedNumberOfComponents = 4;

        private List<IMessageComponent> _components;
        private int _length;

        public MessageComposer()
            : this(DefaultExpectedNumberOfComponents) { }

        public MessageComposer(int expectedNumberOfComponents)
        {
            _components = new List<IMessageComponent>(expectedNumberOfComponents);
            _length = 0;
        }

        public void Write(byte[] buffer)
        {
            AddComponent(new BufferMessageComponent(buffer));
        }

        public void Write(int value)
        {
            AddComponent(new IntMessageComponent(value));
        }

        public void Write(BitSequence bits)
        {
            AddComponent(new BitSequenceMessageComponent(bits));
        }

        public void Write(BitMatrix bits)
        {
            AddComponent(new BitMatrixMessageComponent(bits));
        }

        private void AddComponent(IMessageComponent component)
        {
            _components.Add(component);
            _length += component.Length;
        }

        public byte[] Compose()
        {
            byte[] messageBuffer = new byte[_length];

            int offset = 0;
            foreach (IMessageComponent component in _components)
                component.WriteToBuffer(messageBuffer, ref offset);

            return messageBuffer;
        }

        public int Length
        {
            get
            {
                return _length;
            }
        }
    }
}
