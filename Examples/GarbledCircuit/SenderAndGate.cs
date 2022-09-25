using System;
using System.Security.Cryptography;
using System.Collections.Generic;

using CompactOT;
using CompactOT.DataStructures;

namespace CompactOT.Examples.GarbledCircuit
{

    class SenderAndGate
    {
        private RandomNumberGenerator _randomNumberGenerator;

        private BitSequence _wireValueDelta;

        private GenericDoubleInputGate? _gate;

        public SenderAndGate(RandomNumberGenerator randomNumberGenerator, BitSequence wireValueDelta)
        {
            _randomNumberGenerator = randomNumberGenerator;
            _wireValueDelta = wireValueDelta;
        }

        public BitSequence Apply(BitSequence x, BitSequence y)
        {
            int wireValueLength = x.Length;
            var outWireZeroValue = _randomNumberGenerator.GetBits(wireValueLength);

            _gate = GenericDoubleInputGate.MakeAnd(x, y, outWireZeroValue, _wireValueDelta);

            return outWireZeroValue;
        }

        public BitSequence SerializeToBits(RandomNumberGenerator randomNumberGenerator)
        {
            if (_gate == null)
                throw new NotSupportedException();

            return _gate.SerializeToBits(randomNumberGenerator);
        }

        public byte[] SerializeToBytes(RandomNumberGenerator randomNumberGenerator)
        {
            if (_gate == null)
                throw new NotSupportedException();

            return _gate.SerializeToBytes(randomNumberGenerator);
        }
    }

}
