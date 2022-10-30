// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using Xunit;
using System;

namespace CompactOT.DataStructures
{
    public class BitMatrixTests
    {
        [Fact]
        public void TestConstructor()
        {
            int numberOfRows = 3;
            int numberOfColumns = 82;
            var matrix = new BitMatrix(numberOfRows, numberOfColumns);

            Assert.Equal(numberOfRows, matrix.Rows);
            Assert.Equal(numberOfColumns, matrix.Cols);
            Assert.Equal(numberOfRows * numberOfColumns, matrix.Length);
        }

        [Fact]
        public void TestConstructorInvalidRows()
        {
            Assert.Throws<ArgumentException>(() => new BitMatrix(-1, 2));
        }

        [Fact]
        public void TestConstructorInvalidColumns()
        {
            Assert.Throws<ArgumentException>(() => new BitMatrix(1, -1));
        }

        [Fact]
        public void TestValuesConstructorAndGetRows()
        {
            int numberOfRows = 4;
            int numberOfColumns = 2;
            var values = BitArray.FromBinaryString("00011011");
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, values);

            Assert.Equal(numberOfRows, matrix.Rows);
            Assert.Equal(numberOfColumns, matrix.Cols);

            var expectedRows = new BitArray[] {
                BitArray.FromBinaryString("00"),
                BitArray.FromBinaryString("01"),
                BitArray.FromBinaryString("10"),
                BitArray.FromBinaryString("11")
            };

            for (int i = 0; i < numberOfRows; ++i)
            {
                Assert.Equal(expectedRows[i], matrix.GetRow(i));
            }
        }

        [Fact]
        public void TestValuesConstructorUnmatchingValues()
        {
            int numberOfRows = 4;
            int numberOfColumns = 2;
            var values = BitArray.FromBinaryString("00011");

            Assert.Throws<ArgumentException>(() => new BitMatrix(numberOfRows, numberOfColumns, values));
        }

        [Fact]
        public void TestValuesConstructorAndSetRows()
        {
            int numberOfRows = 4;
            int numberOfColumns = 2;
            var values = BitArray.FromBinaryString("00011011");
            var oldValues = values.Clone();
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, values);

            var newRow = BitArray.FromBinaryString("10");

            matrix.SetRow(1, newRow);

            var expectedRows = new BitArray[] {
                BitArray.FromBinaryString("00"),
                BitArray.FromBinaryString("10"),
                BitArray.FromBinaryString("10"),
                BitArray.FromBinaryString("11")
            };
            

            for (int i = 0; i < numberOfRows; ++i)
            {
                Assert.Equal(expectedRows[i], matrix.GetRow(i));
            }

            Assert.Equal(oldValues, values); // ensure that changing matrix does not change initial array
        }

        [Fact]
        public void TestGetRowIndexTooLarge()
        {
            int numberOfRows = 4;
            int numberOfColumns = 2;
            var matrix = new BitMatrix(numberOfRows, numberOfColumns);

            Assert.Throws<ArgumentOutOfRangeException>(() => matrix.GetRow(numberOfRows));
        }

        [Fact]
        public void TestGetRowIndexNegative()
        {
            int numberOfRows = 4;
            int numberOfColumns = 2;
            var matrix = new BitMatrix(numberOfRows, numberOfColumns);

            Assert.Throws<ArgumentOutOfRangeException>(() => matrix.GetRow(-1));
        }

        [Fact]
        public void TestSetRowIndexTooLarge()
        {
            int numberOfRows = 4;
            int numberOfColumns = 2;
            var matrix = new BitMatrix(numberOfRows, numberOfColumns);
            var newRow = BitArray.FromBinaryString("00");

            Assert.Throws<ArgumentOutOfRangeException>(() => matrix.SetRow(numberOfRows, newRow));
        }

        [Fact]
        public void TestSetRowIndexNegative()
        {
            int numberOfRows = 4;
            int numberOfColumns = 2;
            var matrix = new BitMatrix(numberOfRows, numberOfColumns);
            var newRow = BitArray.FromBinaryString("00");

            Assert.Throws<ArgumentOutOfRangeException>(() => matrix.SetRow(-1, newRow));
        }

        [Fact]
        public void TestSetRowMismatchingValues()
        {
            int numberOfRows = 4;
            int numberOfColumns = 2;
            var matrix = new BitMatrix(numberOfRows, numberOfColumns);
            var newRow = BitArray.FromBinaryString("001");

            Assert.Throws<ArgumentException>(() => matrix.SetRow(0, newRow));
        }

        [Fact]
        public void TestGetColumn()
        {
            int numberOfRows = 4;
            int numberOfColumns = 2;
            var values = BitArray.FromBinaryString("00011011");
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, values);

            var expectedColumns = new BitArray[] {
                BitArray.FromBinaryString("0011"),
                BitArray.FromBinaryString("0101")
            };
            
            for (int i = 0; i < numberOfColumns; ++i)
            {
                Assert.Equal(expectedColumns[i], matrix.GetColumn(i));
            }
        }

        [Fact]
        public void TestSetColumn()
        {
            int numberOfRows = 4;
            int numberOfColumns = 2;
            var values = BitArray.FromBinaryString("00011011");
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, values);

            var newColumn = BitArray.FromBinaryString("1101");
            matrix.SetColumn(1, newColumn);

            var expectedColumns = new BitArray[] {
                BitArray.FromBinaryString("0011"),
                BitArray.FromBinaryString("1101")
            };
            
            for (int i = 0; i < numberOfColumns; ++i)
            {
                Assert.Equal(expectedColumns[i], matrix.GetColumn(i));
            }
        }

        [Fact]
        public void TestGetColumnIndexTooLarge()
        {
            int numberOfRows = 4;
            int numberOfColumns = 2;
            var matrix = new BitMatrix(numberOfRows, numberOfColumns);

            Assert.Throws<ArgumentOutOfRangeException>(() => matrix.GetColumn(numberOfColumns));
        }

        [Fact]
        public void TestGetColumnIndexNegative()
        {
            int numberOfRows = 4;
            int numberOfColumns = 2;
            var matrix = new BitMatrix(numberOfRows, numberOfColumns);

            Assert.Throws<ArgumentOutOfRangeException>(() => matrix.GetColumn(-1));
        }

        [Fact]
        public void TestSetColumnIndexTooLarge()
        {
            int numberOfRows = 4;
            int numberOfColumns = 2;
            var matrix = new BitMatrix(numberOfRows, numberOfColumns);
            var newColumn = BitArray.FromBinaryString("0000");

            Assert.Throws<ArgumentOutOfRangeException>(() => matrix.SetColumn(numberOfColumns, newColumn));
        }

        [Fact]
        public void TestSetColumnIndexNegative()
        {
            int numberOfRows = 4;
            int numberOfColumns = 2;
            var matrix = new BitMatrix(numberOfRows, numberOfColumns);
            var newColumn = BitArray.FromBinaryString("0000");

            Assert.Throws<ArgumentOutOfRangeException>(() => matrix.SetColumn(-1, newColumn));
        }

        [Fact]
        public void TestSetColumnMismatchingValues()
        {
            int numberOfRows = 4;
            int numberOfColumns = 2;
            var matrix = new BitMatrix(numberOfRows, numberOfColumns);
            var newColumn = BitArray.FromBinaryString("001");

            Assert.Throws<ArgumentException>(() => matrix.SetColumn(0, newColumn));
        }

        [Fact]
        public void TestGetElement()
        {
            int numberOfRows = 4;
            int numberOfColumns = 3;
            var values = BitArray.FromBinaryString("000001010111");
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, values);

            Assert.Equal(Bit.Zero, matrix[0,0]);
            Assert.Equal(Bit.Zero, matrix[0,1]);
            Assert.Equal(Bit.Zero, matrix[1,1]);
            Assert.Equal(Bit.One, matrix[1,2]);
            Assert.Equal(Bit.Zero, matrix[2,0]);
            Assert.Equal(Bit.One, matrix[3,0]);
            Assert.Equal(Bit.One, matrix[3,2]);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        [InlineData(2, 0)]
        [InlineData(3, 0)]
        [InlineData(3, 2)]
        public void TestSetElement(int row, int column)
        {
            int numberOfRows = 4;
            int numberOfColumns = 3;
            var values = BitArray.FromBinaryString("000001010111");
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, values);
            var oldMatrix = new BitMatrix(numberOfRows, numberOfColumns, values);

            matrix[row, column] = ~matrix[row, column];

            for (int i = 0; i < numberOfRows; ++i)
            {
                for (int j = 0; j < numberOfColumns; ++j)
                {
                    if (i == row && j == column)
                    {
                        matrix[i, j] = ~oldMatrix[row, column];
                    }
                    else
                    {
                        matrix[i, j] = oldMatrix[row, column];
                    }
                }
            }
        }

        [Fact]
        public void TestGetElementRowIndexNegative()
        {
            int numberOfRows = 4;
            int numberOfColumns = 3;
            var values = BitArray.FromBinaryString("000001010111");
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, values);

            Assert.Throws<ArgumentOutOfRangeException>(() => matrix[-1, 2]);
        }

        [Fact]
        public void TestGetElementRowIndexTooLarge()
        {
            int numberOfRows = 4;
            int numberOfColumns = 3;
            var values = BitArray.FromBinaryString("000001010111");
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, values);

            Assert.Throws<ArgumentOutOfRangeException>(() => matrix[numberOfRows, 2]);
        }

        [Fact]
        public void TestSetElementRowIndexNegative()
        {
            int numberOfRows = 4;
            int numberOfColumns = 3;
            var values = BitArray.FromBinaryString("000001010111");
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, values);

            Assert.Throws<ArgumentOutOfRangeException>(() => matrix[-1, 2] = Bit.Zero);
        }

        [Fact]
        public void TestSetElementRowIndexTooLarge()
        {
            int numberOfRows = 4;
            int numberOfColumns = 3;
            var values = BitArray.FromBinaryString("000001010111");
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, values);

            Assert.Throws<ArgumentOutOfRangeException>(() => matrix[numberOfRows, 2] = Bit.Zero);
        }

        [Fact]
        public void TestGetElementColumnIndexNegative()
        {
            int numberOfRows = 4;
            int numberOfColumns = 3;
            var values = BitArray.FromBinaryString("000001010111");
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, values);

            Assert.Throws<ArgumentOutOfRangeException>(() => matrix[0, -1]);
        }

        [Fact]
        public void TestGetElementColumnIndexTooLarge()
        {
            int numberOfRows = 4;
            int numberOfColumns = 3;
            var values = BitArray.FromBinaryString("000001010111");
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, values);

            Assert.Throws<ArgumentOutOfRangeException>(() => matrix[0, numberOfColumns]);
        }

        [Fact]
        public void TestSetElementColumnIndexNegative()
        {
            int numberOfRows = 4;
            int numberOfColumns = 3;
            var values = BitArray.FromBinaryString("000001010111");
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, values);

            Assert.Throws<ArgumentOutOfRangeException>(() => matrix[0, -1] = Bit.Zero);
        }

        [Fact]
        public void TestSetElementColumnIndexTooLarge()
        {
            int numberOfRows = 4;
            int numberOfColumns = 3;
            var values = BitArray.FromBinaryString("000001010111");
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, values);

            Assert.Throws<ArgumentOutOfRangeException>(() => matrix[0, numberOfColumns] = Bit.Zero);
        }

        [Fact]
        public void TestAsFlat()
        {
            int numberOfRows = 4;
            int numberOfColumns = 3;
            var values = BitArray.FromBinaryString("000001010111");
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, values);

            var result = matrix.AsFlat();
            Assert.Equal(values, result);
        }

        [Fact]
        public void TestOr()
        {
            int numberOfRows = 4;
            int numberOfColumns = 3;
            var leftValues = BitArray.FromBinaryString("000001010111");
            var leftMatrix = new BitMatrix(numberOfRows, numberOfColumns, leftValues);

            var rightValues = BitArray.FromBinaryString("010101000101");
            var rightMatrix = new BitMatrix(numberOfRows, numberOfColumns, rightValues);

            var expectedValues = BitArray.FromBinaryString("010101010111");
            var expectedMatrix = new BitMatrix(numberOfRows, numberOfColumns, expectedValues);

            var result = leftMatrix.Or(rightMatrix);
            Assert.Equal(expectedMatrix, result);

            var operatorResult = leftMatrix | rightMatrix;
            Assert.Equal(result, operatorResult);
        }

        [Fact]
        public void TestXor()
        {
            int numberOfRows = 4;
            int numberOfColumns = 3;
            var leftValues = BitArray.FromBinaryString("000001010111");
            var leftMatrix = new BitMatrix(numberOfRows, numberOfColumns, leftValues);

            var rightValues = BitArray.FromBinaryString("010101000101");
            var rightMatrix = new BitMatrix(numberOfRows, numberOfColumns, rightValues);

            var expectedValues = BitArray.FromBinaryString("010100010010");
            var expectedMatrix = new BitMatrix(numberOfRows, numberOfColumns, expectedValues);

            var result = leftMatrix.Xor(rightMatrix);

            Assert.Equal(expectedMatrix, result);

            var operatorResult = leftMatrix ^ rightMatrix;
            Assert.Equal(result, operatorResult);
        }

        [Fact]
        public void TestAnd()
        {
            int numberOfRows = 4;
            int numberOfColumns = 3;
            var leftValues = BitArray.FromBinaryString("000001010111");
            var leftMatrix = new BitMatrix(numberOfRows, numberOfColumns, leftValues);

            var rightValues = BitArray.FromBinaryString("010101000101");
            var rightMatrix = new BitMatrix(numberOfRows, numberOfColumns, rightValues);

            var expectedValues = BitArray.FromBinaryString("000001000101");
            var expectedMatrix = new BitMatrix(numberOfRows, numberOfColumns, expectedValues);

            var result = leftMatrix.And(rightMatrix);

            Assert.Equal(expectedMatrix, result);

            var operatorResult = leftMatrix & rightMatrix;
            Assert.Equal(result, operatorResult);
        }

        [Fact]
        public void TestNot()
        {
            int numberOfRows = 4;
            int numberOfColumns = 3;
            var values = BitArray.FromBinaryString("010101000101");
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, values);

            var expectedValues = BitArray.FromBinaryString("101010111010");
            var expectedMatrix = new BitMatrix(numberOfRows, numberOfColumns, expectedValues);

            var result = matrix.Not();

            Assert.Equal(expectedMatrix, result);

            var operatorResult = ~matrix;
            Assert.Equal(result, operatorResult);
        }

        [Fact]
        public void TestOrDimensionMismatch()
        {
            int numberOfRows = 4;
            int numberOfColumns = 3;
            var leftValues = BitArray.FromBinaryString("000001010111");
            var leftMatrix = new BitMatrix(numberOfRows, numberOfColumns, leftValues);

            var rightValues = BitArray.FromBinaryString("010101000101");
            var rightMatrix = new BitMatrix(numberOfColumns, numberOfRows, rightValues);

            Assert.Throws<ArgumentException>(() => leftMatrix.Or(rightMatrix));
        }

        [Fact]
        public void TestXorDimensionMismatch()
        {
            int numberOfRows = 4;
            int numberOfColumns = 3;
            var leftValues = BitArray.FromBinaryString("000001010111");
            var leftMatrix = new BitMatrix(numberOfRows, numberOfColumns, leftValues);

            var rightValues = BitArray.FromBinaryString("010101000101");
            var rightMatrix = new BitMatrix(numberOfColumns, numberOfRows, rightValues);

            Assert.Throws<ArgumentException>(() => leftMatrix.Xor(rightMatrix));
        }

        [Fact]
        public void TestAndDimensionMismatch()
        {
            int numberOfRows = 4;
            int numberOfColumns = 3;
            var leftValues = BitArray.FromBinaryString("000001010111");
            var leftMatrix = new BitMatrix(numberOfRows, numberOfColumns, leftValues);

            var rightValues = BitArray.FromBinaryString("010101000101");
            var rightMatrix = new BitMatrix(numberOfColumns, numberOfRows, rightValues);

            Assert.Throws<ArgumentException>(() => leftMatrix.And(rightMatrix));
        }

        [Fact]
        public void TestEquals()
        {
            int numberOfRows = 2;
            int numberOfColumns = 2;
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, BitArray.FromBinaryString("1001"));

            var other = new BitMatrix(numberOfRows, numberOfColumns, BitArray.FromBinaryString("1001"));

            Assert.True(matrix.Equals(other));
        }

        [Fact]
        public void TestEqualsDifferentValues()
        {
            int numberOfRows = 2;
            int numberOfColumns = 2;
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, BitArray.FromBinaryString("1001"));

            var other = new BitMatrix(numberOfRows, numberOfColumns, BitArray.FromBinaryString("1010"));

            Assert.False(matrix.Equals(other));
        }
        
        [Fact]
        public void TestEqualsDifferentRows()
        {
            int numberOfRows = 2;
            int numberOfColumns = 2;
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, BitArray.FromBinaryString("1001"));

            var other = new BitMatrix(1, numberOfColumns, BitArray.FromBinaryString("10"));

            Assert.False(matrix.Equals(other));
        }

        [Fact]
        public void TestEqualsDifferentColumns()
        {
            int numberOfRows = 2;
            int numberOfColumns = 2;
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, BitArray.FromBinaryString("1001"));

            var other = new BitMatrix(numberOfRows, 1, BitArray.FromBinaryString("10"));

            Assert.False(matrix.Equals(other));
        }

        [Fact]
        public void TestEqualsUnrelatedObject()
        {
            int numberOfRows = 2;
            int numberOfColumns = 2;
            var matrix = new BitMatrix(numberOfRows, numberOfColumns, BitArray.FromBinaryString("1001"));

            var other = new object();

            Assert.False(matrix.Equals(other));
        }

        

        
        

   }
}
