using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

using CompactOT.DataStructures;

namespace CompactOT.Examples.GarbledCircuit
{

    class FreeXorGate : DoubleInputGate
    {
        public BitSequence Apply(BitSequence firstInput, BitSequence secondInput)
        {
            return firstInput ^ secondInput;
        }

    }

}
