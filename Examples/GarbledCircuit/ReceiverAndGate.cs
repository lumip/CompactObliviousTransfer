using System;

using CompactOT;
using CompactOT.DataStructures;

namespace CompactOT.Examples.GarbledCircuit
{

    class ReceiverAndGate
    {
        private GenericDoubleInputGate _gate;

        public static ReceiverAndGate Deserialize(byte[] bytes)
        {
            return new ReceiverAndGate(GenericDoubleInputGate.Deserialize(bytes));
        }

        private ReceiverAndGate(GenericDoubleInputGate gate)
        {
            _gate = gate;
        }

        public BitSequence Apply(BitSequence x, BitSequence y)
        {
            return _gate.Apply(x, y);
        }
    }

}
