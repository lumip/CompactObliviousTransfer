using CompactOT.DataStructures;

namespace CompactOT.Buffers.Internal
{
    public class BitArrayMessageComponent : IMessageComponent
    {

        IBitArray _array;

        public BitArrayMessageComponent(IBitArray array)
        {
            _array = array;
        }
        
        public int Length => BitArray.RequiredBytes(_array.Length);

        public void WriteToBuffer(byte[] messageBuffer, ref int offset)
        {
            _array.CopyTo(messageBuffer, offset);
            offset += Length;
        }

        public static IBitArray ReadFromBuffer(byte[] messageBuffer, ref int offset, int numberOfElements)
        {
            var bits = BitArray.FromBytes(messageBuffer, numberOfElements, offset);
            offset += BitArray.RequiredBytes(numberOfElements);
            return bits;
        }
    }
}