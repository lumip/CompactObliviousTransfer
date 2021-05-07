using System;

using CompactOT.DataStructures;

namespace CompactOT
{

    public static class WalshHadamardCode
    {
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

        public static BitArray ComputeWalshHadamardCode(int x, int codeLength)
        {
            if ((codeLength & (codeLength - 1)) != 0)
                throw new ArgumentException($"Code length must be a power of two, was {codeLength}.", nameof(codeLength));
            var code = new BitArray(codeLength);
            
            for (int i = 0; i < codeLength; ++i)
            {
                code[i] = new Bit(GetParity(x & i));
            }
            return code;
        }
    }

}