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
