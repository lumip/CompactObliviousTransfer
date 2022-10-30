// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Linq;
using System.Collections.Generic;

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

        public BitMatrix(int numberOfRows, int numberOfColumns, BitSequence values) : this(numberOfRows, numberOfColumns)
        {
            if (values.Length != Length)
                throw new ArgumentException("Argument must be of the correct dimensions!", nameof(values));

            _values = new BitArray(values);
        }

        public static BitMatrix Zeros(int numberOfRows, int numberOfColumns)
        {
            return new BitMatrix(
                numberOfRows,
                numberOfColumns,
                new ConstantBitArrayView(Bit.Zero, numberOfRows * numberOfColumns)
            );
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

        public BitSequence GetRow(int row)
        {
            if (row < 0 || row >= Rows)
                throw new ArgumentOutOfRangeException(nameof(row));
            int rowIndex = GetValuesIndex(row, 0);
            return new BitArraySlice(_values, rowIndex, rowIndex + Cols);
        }

        public void SetRow(int row, BitSequence values)
        {
            if (row < 0 || row >= Rows)
                throw new ArgumentOutOfRangeException(nameof(row));
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (values.Length != Cols)
                throw new ArgumentException("Provided argument must match the number of columns.", nameof(values));
            foreach ((int j, Bit v) in values.Enumerate())
            {
                _values[GetValuesIndex(row, j)] = v;
            }
        }

        private IEnumerable<Bit> GetColumnAsEnumerable(int col)
        {
            for (int i = 0; i < Rows; ++i)
            {
                yield return _values[GetValuesIndex(i, col)];
            }
        }

        public BitSequence GetColumn(int col)
        {
            if (col < 0 || col >= Cols)
                throw new ArgumentOutOfRangeException(nameof(col));

            return new EnumeratedBitArrayView(GetColumnAsEnumerable(col), Rows);
        }

        public void SetColumn(int col, BitSequence values)
        {
            if (col < 0 || col >= Cols)
                throw new ArgumentOutOfRangeException(nameof(col));
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (values.Length != Rows)
                throw new ArgumentException("Provided argument must match the number of columns.", nameof(values));
            foreach ((int i, Bit v) in values.Enumerate())
            {
                _values[GetValuesIndex(i, col)] = v;
            }
        }

        public BitMatrix And(BitMatrix other)
        {
            if (Rows != other.Rows || Cols != other.Cols)
                throw new ArgumentException("And operator can only be applied for two matrices of the same size.", nameof(other));
            return new BitMatrix(Rows, Cols, _values & other._values);
        }

        public BitMatrix Or(BitMatrix other)
        {
            if (Rows != other.Rows || Cols != other.Cols)
                throw new ArgumentException("Or operator can only be applied for two matrices of the same size.", nameof(other));
            return new BitMatrix(Rows, Cols, _values | other._values);
        }

        public BitMatrix Xor(BitMatrix other)
        {
            if (Rows != other.Rows || Cols != other.Cols)
                throw new ArgumentException("Xor operator can only be applied for two matrices of the same size.", nameof(other));
            return new BitMatrix(Rows, Cols, _values ^ other._values);
        }

        public BitMatrix Not()
        {
            return new BitMatrix(Rows, Cols, ~_values);
        }

        public static BitMatrix operator &(BitMatrix left, BitMatrix right) => left.And(right);
        public static BitMatrix operator |(BitMatrix left, BitMatrix right) => left.Or(right);
        public static BitMatrix operator ^(BitMatrix left, BitMatrix right) => left.Xor(right);

        public static BitMatrix operator ~(BitMatrix matrix) => matrix.Not();

        public BitSequence AsFlat()
        {
            return _values;
        }

        public override bool Equals(object obj)
        {
            BitMatrix? other = obj as BitMatrix;
            if (other == null)
                return false;

            return Rows == other.Rows && Cols == other.Cols && _values.Equals(other._values);
        }

        public override int GetHashCode()
        {
            return 15527 * Rows + 37307 * Cols + 8599 * Rows.GetHashCode();
        }

    }
}
