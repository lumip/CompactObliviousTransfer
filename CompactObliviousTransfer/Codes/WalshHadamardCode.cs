using System;
using System.Collections.Generic;

using CompactOT.DataStructures;
using CompactCryptoGroupAlgebra;

namespace CompactOT.Codes
{

    public class WalshHadamardCode : IBinaryCode
    {

        public int CodeLength { get; }

        public int Distance => CodeLength >> 1;

        public int MaximumMessage => CodeLength - 1; 

        public WalshHadamardCode(int codeLength)
        {
            if (!MathUtil.IsPowerOfTwo(codeLength))
                throw new ArgumentException($"Code length must be a power of two, was {codeLength}.", nameof(codeLength));

            CodeLength = codeLength;
        }

        public static WalshHadamardCode CreateWithDistance(int distance)
        {
            int codeLength = MathUtil.NextPowerOfTwo(distance) << 1;
            if (codeLength <= distance)
                throw new ArgumentOutOfRangeException("Cannot instantiate a Walsh-Hadamard code with distance larger than 2^31 - 1.");
            return new WalshHadamardCode(codeLength);
        }

        public static WalshHadamardCode CreateWithMaximumMessage(int maximumMessage)
        {
            int codeLength = MathUtil.NextPowerOfTwo(maximumMessage);
            return new WalshHadamardCode(codeLength);
        }

        public static byte GetParity(int x)
        {
            int p = x;
            p ^= p >> 16;
            p ^= p >> 8;
            p ^= p >> 4;
            p ^= p >> 2;
            p ^= p >> 1;
            return (byte)(p & 1);
        }

        private IEnumerable<Bit> EncodeToEnumerable(int x)
        {
            for (int i = 0; i < CodeLength; ++i)
            {
                yield return new Bit(GetParity(x & i));
            }
        }

        public BitSequence Encode(int x)
        {
            if (x > MaximumMessage)
            {
                int requiredCodeLength = 1 << NumberLength.GetLength(x).InBits;
                throw new ArgumentOutOfRangeException(
                    $"Provided value {x} is too large to be encoded with a code length of {CodeLength}"+
                    $"(required code length at least {requiredCodeLength}).",
                    nameof(x)
                );
            }
            
            return new EnumeratedBitArrayView(EncodeToEnumerable(x), CodeLength);
        }
    }
}
