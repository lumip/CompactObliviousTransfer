using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

using CompactOT.DataStructures;

namespace CompactOT.Examples.GarbledCircuit
{

    class SingleInputGate
    {
        private Dictionary<BitSequence, BitSequence> _gateLookup;

        public SingleInputGate(Dictionary<BitSequence, BitSequence> gateLookup)
        {
            _gateLookup = gateLookup;
        }

        public BitSequence Apply(BitSequence input)
        {
            return _gateLookup[input];
        }

        public BitSequence SerializeToBits(RandomNumberGenerator randomNumberGenerator)
        {
            BitSequence[] lookupSequences = _gateLookup.Select(item => item.Key.Concatenate(item.Value)).ToArray();

            int flip = (int)randomNumberGenerator.GetBits(1)[0];
            return lookupSequences[flip].Concatenate(lookupSequences[1 - flip]);
        }

        public byte[] SerializeToBytes(RandomNumberGenerator randomNumberGenerator)
        {
            var bits = SerializeToBits(randomNumberGenerator);
            
            var wireValueLength = bits.Length / 4;
            var wireValueLengthBits = BitArray.FromBytes(BitConverter.GetBytes(wireValueLength), 32);

            return BitSequence.Empty.Concatenate(wireValueLengthBits).Concatenate(bits).ToBytes();
        }

        public static SingleInputGate Deserialize(byte[] bytes)
        {
            int wireValueLength = BitConverter.ToInt32(bytes, 0);
            var wireValues = BitArray.FromBytes(bytes, 4 * wireValueLength, 4);
            return Deserialize(wireValues);
        }

        public static SingleInputGate Deserialize(BitSequence bits)
        {
            var wireValues = bits;
            int wireValueLength = bits.Length / 4;
            var firstKey  = new BitArraySlice(wireValues, 0 * wireValueLength, 1 * wireValueLength);
            var firstVal  = new BitArraySlice(wireValues, 1 * wireValueLength, 2 * wireValueLength);
            var secondKey = new BitArraySlice(wireValues, 2 * wireValueLength, 3 * wireValueLength);
            var secondVal = new BitArraySlice(wireValues, 3 * wireValueLength, 4 * wireValueLength);

            return new SingleInputGate(new Dictionary<BitSequence, BitSequence>
                {
                    { firstKey, firstVal },
                    { secondKey, secondVal }
                }
            );
        }

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

    }

}
