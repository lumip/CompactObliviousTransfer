using CompactOT.DataStructures;

namespace CompactOT.Buffers.Internal
{
    public class BitMatrixMessageComponent : IMessageComponent
    {
        private BitMatrix _bits;

        public BitMatrixMessageComponent(BitMatrix bits)
        {
            _bits = bits;
        }

        public int Length => _bits.Rows * BitArray.RequiredBytes(_bits.Cols);

        public void WriteToBuffer(byte[] messageBuffer, ref int offset)
        {
            int bytesPerRow = BitArray.RequiredBytes(_bits.Cols);
            for (int i = 0; i < _bits.Rows; ++i)
            {
                _bits.GetRow(i).CopyTo(messageBuffer, offset);
                offset += bytesPerRow;
            }
        }

        public static BitMatrix ReadFromBuffer(byte[] messageBuffer, ref int offset, int rows, int columns)
        {
            int numberOfElements = rows * columns;
            int bytesPerRow = BitArray.RequiredBytes(columns);

            var bits = new BitMatrix(rows, columns);
            for (int i = 0; i < rows; ++i)
            {
                var row = BitArray.FromBytes(messageBuffer, columns, offset);
                bits.SetRow(i, row);
                offset += bytesPerRow;
            }
            return bits;
        }
    }
}