// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;

using CompactOT.DataStructures;

namespace CompactOT.Examples.GarbledCircuit
{

    class FreeXorGate
    {
        public BitSequence Apply(BitSequence firstInput, BitSequence secondInput)
        {
            return firstInput ^ secondInput;
        }

    }

}
