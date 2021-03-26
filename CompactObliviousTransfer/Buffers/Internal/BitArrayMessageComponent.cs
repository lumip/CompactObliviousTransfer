using CompactOT.DataStructures;

namespace CompactOT.Buffers.Internal
{
    public class BitArrayMessageComponent : IMessageComponent
    {

        BitArray _array;

        public BitArrayMessageComponent(BitArray array)
        {
            _array = array;
        }
        
        public int Length => BitArray.RequiredBytes(_array.Length);

        public void WriteToBuffer(byte[] messageBuffer, ref int offset)
        {
            _array.CopyTo(messageBuffer, offset);
            offset += Length;
        }

        public static BitArray ReadFromBuffer(byte[] messageBuffer, ref int offset, int numberOfElements)
        {
            var bits = BitArray.CreateFromBytes(messageBuffer, numberOfElements, offset);
            offset += BitArray.RequiredBytes(numberOfElements);
            return bits;
        }
    }
}