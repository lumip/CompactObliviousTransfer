using System.Collections.Generic;
using System.Collections;

namespace CompactOT.DataStructures
{
    public interface IBitArray : ICollection, IEnumerable<Bit>
    {
        void CopyTo(byte[] buffer, int offset);
        byte[] ToBytes();

        int Length { get; }

        IBitArray And(IBitArray other);
        IBitArray Or(IBitArray other);
        IBitArray Xor(IBitArray other);
        IBitArray Not();

        IBitArray And(Bit bit);
        IBitArray Or(Bit bit);
        IBitArray Xor(Bit bit);

        IEnumerable<byte> AsByteEnumerable();

        string ToBinaryString();
    }
}