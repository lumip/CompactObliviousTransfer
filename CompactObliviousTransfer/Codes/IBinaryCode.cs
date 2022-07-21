using CompactOT.DataStructures;

namespace CompactOT.Codes
{
    public interface IBinaryCode
    {
        public BitSequence Encode(int x);

        public int CodeLength { get; }
    }

}
