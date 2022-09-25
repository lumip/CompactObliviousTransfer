using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

using CompactOT.DataStructures;

namespace CompactOT.Examples.GarbledCircuit
{

    class GenericDoubleInputGate
    {
        private Dictionary<BitSequence, SingleInputGate> _gateLookup;

        public GenericDoubleInputGate(Dictionary<BitSequence, SingleInputGate> gateLookup)
        {
            _gateLookup = gateLookup;
        }

        public BitSequence Apply(BitSequence firstInput, BitSequence secondInput)
        {
            return _gateLookup[firstInput].Apply(secondInput);
        }

        public BitSequence SerializeToBits(RandomNumberGenerator randomNumberGenerator)
        {
            BitSequence[] lookupSequences = _gateLookup.Select(
                item => item.Key.Concatenate(item.Value.SerializeToBits(randomNumberGenerator))
            ).ToArray();

            int flip = (int)randomNumberGenerator.GetBits(1)[0];
            return lookupSequences[flip].Concatenate(lookupSequences[1 - flip]);
        }

        public byte[] SerializeToBytes(RandomNumberGenerator randomNumberGenerator)
        {
            var bits = SerializeToBits(randomNumberGenerator);
            
            int numWireValues = 10;
            int wireValueLength = bits.Length / numWireValues;
            var wireValueLengthBits = BitArray.FromBytes(BitConverter.GetBytes(wireValueLength), 32);

            return BitSequence.Empty.Concatenate(wireValueLengthBits).Concatenate(bits).ToBytes();
        }

        public static GenericDoubleInputGate Deserialize(BitSequence bits)
        {
            int numWireValues = 10;
            int wireValueLength = bits.Length / numWireValues;

            var firstKey = new BitArraySlice(bits, 0 * wireValueLength, 1 * wireValueLength);
            var firstSubGateBits = new BitArraySlice(bits, 1 * wireValueLength, 5 * wireValueLength);
            var firstSubGate = SingleInputGate.Deserialize(firstSubGateBits);

            var secondKey = new BitArraySlice(bits, 5 * wireValueLength, 6 * wireValueLength);
            var secondSubGateBits = new BitArraySlice(bits, 6 * wireValueLength, 10 * wireValueLength);
            var secondSubGate = SingleInputGate.Deserialize(secondSubGateBits);

            return new GenericDoubleInputGate(new Dictionary<BitSequence, SingleInputGate>
                {
                    { firstKey, firstSubGate },
                    { secondKey, secondSubGate },
                }
            );
        }

        public static GenericDoubleInputGate Deserialize(byte[] bytes)
        {
            int wireValueLength = BitConverter.ToInt32(bytes, 0);
            var bits = BitArray.FromBytes(bytes, wireValueLength * 10, 4);
            return Deserialize(bits);
        }

        public static GenericDoubleInputGate MakeAnd(
            BitSequence firstIn0, BitSequence secondIn0,
            BitSequence out0, BitSequence delta
        )
        {
            return new GenericDoubleInputGate(new Dictionary<BitSequence, SingleInputGate>()
                {
                    { firstIn0, SingleInputGate.MakeConstant(secondIn0, out0, delta) },
                    { firstIn0 ^ delta, SingleInputGate.MakeIdentity(secondIn0, out0, delta) }
                }
            );
        }

    }

}
