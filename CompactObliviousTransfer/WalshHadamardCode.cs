using System.Numerics;

using CompactOT.DataStructures;

namespace CompactOT
{

    public static class WalshHadamardCode
    {
        public static byte GetParity(int x)
        {
            int p = x;
            p ^= p >> 4;
            p ^= p >> 2;
            p ^= p >> 1;
            return (byte)(p & 1);
        }

        public static BitArray ComputeWalshHadamardCode(int x, int numberOfBits)
        {
            int codeLength = 1 << (numberOfBits - 1);
            var code = new BitArray(codeLength);
            
            for (int i = 0; i < (BigInteger.One << codeLength); ++i)
            {
                code[i] = new Bit(GetParity(x ^ i));
            }
            return code;
        }
    }

}