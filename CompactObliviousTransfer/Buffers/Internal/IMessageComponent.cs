// SPDX-FileCopyrightText: 2018 Jonas Nagy-Kuhlen <jonas.nagy-kuhlen@rwth-aachen.de>
// SPDX-License-Identifier: MIT
// Adopted from CompactMPC: https://github.com/jnagykuhlen/CompactMPC

namespace CompactOT.Buffers.Internal
{
    public interface IMessageComponent
    {
        void WriteToBuffer(byte[] messageBuffer, ref int offset);
        int Length { get; }
    }
}
