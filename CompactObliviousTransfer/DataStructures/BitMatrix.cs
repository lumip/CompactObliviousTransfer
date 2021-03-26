using System;

namespace CompactOT.DataStructures
{
    /// <summary>
    /// A two-dimensional storage of bits.
    /// </summary>
    public class BitMatrix
    {
        private BitArray _values;

        public int Rows { get; }
        public int Cols { get; }
        public int Length => Rows * Cols;

        public BitMatrix(int numberOfRows, int numberOfColumns)
        {
            if (numberOfRows < 0)
                throw new ArgumentException("Number of rows must be positive.", nameof(numberOfRows));
            if (numberOfColumns < 0)
                throw new ArgumentException("Number of columns must be positive.", nameof(numberOfColumns));
            Rows = numberOfRows;
            Cols = numberOfColumns;
            _values = new BitArray(Length);
        }

        public BitMatrix(int numberOfRows, int numberOfColumns, BitArray values) : this(numberOfRows, numberOfColumns)
        {
            if (values.Length != Length)
                throw new ArgumentException("Argument must be of the correct dimensions!", nameof(values));

            _values = values.Clone();
        }

        private int GetValuesIndex(int row, int col)
        {
            return (row * Cols + col);
        }

        public Bit this[int row, int col]
        {
            get
            {
                if (row < 0 || row >= Rows)
                    throw new ArgumentOutOfRangeException(nameof(row));
                if (col < 0 || col >= Cols)
                    throw new ArgumentOutOfRangeException(nameof(col));
                return _values[GetValuesIndex(row, col)];
            }
            set
            {
                if (row < 0 || row >= Rows)
                    throw new ArgumentOutOfRangeException(nameof(row));
                if (col < 0 || col >= Cols)
                    throw new ArgumentOutOfRangeException(nameof(col));
                _values[GetValuesIndex(row, col)] = value;
            }
        }

        public BitArray GetRow(int row)
        {
            if (row < 0 || row >= Rows)
                throw new ArgumentOutOfRangeException(nameof(row));
            BitArray result = new BitArray(Cols);
            for (int j = 0; j < Cols; ++j)
            {
                result[j] = _values[GetValuesIndex(row, j)];
            }
            return result;
        }

        public void SetRow(int row, BitArray values)
        {
            if (row < 0 || row >= Rows)
                throw new ArgumentOutOfRangeException(nameof(row));
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (values.Length != Cols)
                throw new ArgumentException("Provided argument must match the number of columns.", nameof(values));
            for (int j = 0; j < Cols; ++j)
            {
                _values[GetValuesIndex(row, j)] = values[j];
            }
        }

        public BitArray GetColumn(int col)
        {
            if (col < 0 || col >= Cols)
                throw new ArgumentOutOfRangeException(nameof(col));
            BitArray result = new BitArray(Rows);
            for (int i = 0; i < Rows; ++i)
            {
                result[i] = _values[GetValuesIndex(i, col)];
            }
            return result;
        }

        public void SetColumn(int col, BitArray values)
        {
            if (col < 0 || col >= Cols)
                throw new ArgumentOutOfRangeException(nameof(col));
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (values.Length != Rows)
                throw new ArgumentException("Provided argument must match the number of columns.", nameof(values));
            for (int i = 0; i < Rows; ++i)
            {
                _values[GetValuesIndex(i, col)] = values[i];
            }
        }

        public BitMatrix Transposed
        {
            get
            {
                BitMatrix transposed = new BitMatrix(Cols, Rows);
                for (int i = 0; i < Rows; ++i)
                {
                    transposed.SetColumn(i, this.GetRow(i));
                }
                return transposed;
            }
        }

    }
}
