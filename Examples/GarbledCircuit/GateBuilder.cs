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
    static class GateBuilder
    {
        
        public static SingleInputGate MakeConstant(BitSequence in0, BitSequence out_, BitSequence delta)
        {
            return new SingleInputGate(new Dictionary<BitSequence, BitSequence>()
                {
                    { in0, out_ },
                    { in0 ^ delta, out_ }
                }
            );
        }

        public static SingleInputGate MakeIdentity(BitSequence in0, BitSequence out0, BitSequence delta)
        {
            return new SingleInputGate(new Dictionary<BitSequence, BitSequence>()
                {
                    { in0, out0 },
                    { in0 ^ delta, out0 ^ delta }
                }
            );
        }

        public static SingleInputGate MakeInverter(BitSequence in0, BitSequence out0, BitSequence delta)
        {
            return MakeIdentity(in0, out0 ^ delta, delta);
        }

        public static BitSequence MakeFreeXor(BitSequence firstIn0, BitSequence secondIn0)
        {
            return firstIn0 ^ secondIn0;
        }

        public static GenericDoubleInputGate MakeAnd(
            BitSequence firstIn0, BitSequence secondIn0,
            BitSequence out0, BitSequence delta
        )
        {
            return new GenericDoubleInputGate(new Dictionary<BitSequence, SingleInputGate>()
                {
                    { firstIn0, MakeConstant(secondIn0, out0, delta) },
                    { firstIn0 ^ delta, MakeIdentity(secondIn0, out0, delta) }
                }
            );
        }

    }
}
