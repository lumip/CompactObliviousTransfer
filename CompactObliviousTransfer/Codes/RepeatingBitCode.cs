using System;
using CompactOT.DataStructures;

namespace CompactOT.Codes
{

    public class RepeatingBitCode : IBinaryCode
    {
        public int CodeLength { get; }

        public int Distance => CodeLength;

        public int MaximumMessage => 1;

        public RepeatingBitCode(int codeLength)
        {
            CodeLength = codeLength;
        }

        public BitSequence Encode(int x)
        {
            if (x > MaximumMessage)
                throw new ArgumentOutOfRangeException($"Repeating bit code only supports binary values, got {x}.", nameof(x));

            return new ConstantBitArrayView(x == 1 ? Bit.One : Bit.Zero, CodeLength);
        }

        public static RepeatingBitCode CreateWithDistance(int distance)
        {
            return new RepeatingBitCode(distance);
        }
    }

}
