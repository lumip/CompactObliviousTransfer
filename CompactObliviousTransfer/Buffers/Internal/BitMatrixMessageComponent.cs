// SPDX-FileCopyrightText: 2018 Jonas Nagy-Kuhlen <jonas.nagy-kuhlen@rwth-aachen.de>, 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: MIT
// Adopted from CompactMPC: https://github.com/jnagykuhlen/CompactMPC

using CompactOT.DataStructures;

namespace CompactOT.Buffers.Internal
{
    public class BitMatrixMessageComponent : BitSequenceMessageComponent
    {

        public BitMatrixMessageComponent(BitMatrix bits) : base(bits.AsFlat()) { }


        public static BitMatrix ReadFromBuffer(byte[] messageBuffer, ref int offset, int rows, int columns)
        {
            int numberOfElements = rows * columns;
            var bits = BitSequenceMessageComponent.ReadFromBuffer(messageBuffer, ref offset, numberOfElements);

            return new BitMatrix(rows, columns, bits);
        }
    }
}
