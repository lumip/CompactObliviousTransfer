// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;

using Xunit;
using Moq;

namespace CompactOT.DataStructures
{
    public class BitSequenceTests
    {
        
        [Fact]
        public void TestToInt32()
        {
            var bytes = new byte[] { 0x27, 0x26, 0xA3, 0xA };

            var bitsMock = new Mock<BitSequence>() { CallBase = true };
            bitsMock.Setup(b => b.ToBytes()).Returns(bytes);

            int expected = 178464295;
            int result = bitsMock.Object.ToInt32();

            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestToInt32TooLongSequence()
        {
            var bitsMock = new Mock<BitSequence>() { CallBase = true };
            bitsMock.Setup(b => b.Length).Returns(33);

            Assert.Throws<InvalidOperationException>(() => bitsMock.Object.ToInt32());
        }

        [Fact]
        public void TestToBigIntegerForceUnsigned()
        {
            var bytes = new byte[] { 0x27, 0x26, 0xA3, 0xAF, 0x70, 0x99 };

            var bitsMock = new Mock<BitSequence>() { CallBase = true };
            bitsMock.Setup(b => b.AsByteEnumerable()).Returns(bytes);
            bitsMock.Setup(b => b.Length).Returns(48);

            var expected = new BigInteger(0x9970AFA32627);
            var result = bitsMock.Object.ToBigInteger(unsigned: true);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestToBigInteger()
        {
            var bytes = new byte[] { 0x27, 0x26, 0xA3, 0xAF, 0x70, 0x99 };

            var bitsMock = new Mock<BitSequence>() { CallBase = true };
            bitsMock.Setup(b => b.AsByteEnumerable()).Returns(bytes);
            bitsMock.Setup(b => b.Length).Returns(48);

            long expectedAbsoluteValue = 0x668F505CD9D9;
            var expected = new BigInteger(-expectedAbsoluteValue);
            var result = bitsMock.Object.ToBigInteger();

            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestCopyToBytesNoOffset()
        {
            var bitsMock = new Mock<BitSequence>() { CallBase = true };
            bitsMock.Setup(b => b.AsByteEnumerable()).Returns(new byte[] { 0x1f, 0x01 });
            var bits = bitsMock.Object;

            var expectedBytes = new byte[] { 0x1f, 0x01, 0x0 };

            byte[] bytes = new byte[3];
            bytes[2] = 0;
            bits.CopyTo(bytes, 0);

            Assert.Equal(expectedBytes, bytes);
        }

        [Fact]
        public void TestCopyToBytesOffset()
        {
            var bitsMock = new Mock<BitSequence>() { CallBase = true };
            bitsMock.Setup(b => b.AsByteEnumerable()).Returns(new byte[] { 0x1f, 0x01 });
            var bits = bitsMock.Object;
            
            var expectedBytes = new byte[] { 0x55, 0x1f, 0x01 };

            byte[] bytes = new byte[3];
            bytes[0] = 0x55;
            bits.CopyTo(bytes, 1);
            
            Assert.Equal(expectedBytes, bytes);
        }

        [Fact]
        public void TestCopyToBytesTooSmallBuffer()
        {
            var bitsMock = new Mock<BitSequence>() { CallBase = true };
            bitsMock.Setup(b => b.AsByteEnumerable()).Returns(new byte[] { 0x1f, 0x01 });
            var bits = bitsMock.Object;
            

            byte[] bytes = new byte[3];
            Assert.Throws<ArgumentException>(() => bits.CopyTo(bytes, 2));
        }

        [Fact]
        public void TestCopyToBitsNoOffset()
        {
            var bitsMock = new Mock<BitSequence>() { CallBase = true };
            bitsMock.Setup(b => b.GetEnumerator()).Returns((new Bit[] { 
                Bit.One, Bit.One, Bit.One, Bit.One,
                Bit.One, Bit.Zero, Bit.Zero, Bit.Zero,
                Bit.One,
            }).AsEnumerable().GetEnumerator());
            var bits = bitsMock.Object;
            
            var expectedBits = new Bit[] {
                Bit.One, Bit.One, Bit.One, Bit.One,
                Bit.One, Bit.Zero, Bit.Zero, Bit.Zero,
                Bit.One, Bit.Zero, Bit.One
            };

            Bit[] buffer = new Bit[11];
            buffer[buffer.Length - 2] = Bit.Zero;
            buffer[buffer.Length - 1] = Bit.One;
            bits.CopyTo(buffer, 0);

            Assert.Equal(expectedBits, buffer);
        }

        [Fact]
        public void TestCopyToBitsOffset()
        {
            var bitsMock = new Mock<BitSequence>() { CallBase = true };
            bitsMock.Setup(b => b.GetEnumerator()).Returns((new Bit[] { 
                Bit.One, Bit.One, Bit.One, Bit.One,
                Bit.One, Bit.Zero, Bit.Zero, Bit.Zero,
                Bit.One,
            }).AsEnumerable().GetEnumerator());
            var bits = bitsMock.Object;
            
            var expectedBits = new Bit[] {
                Bit.One, Bit.Zero, Bit.One, Bit.One,
                Bit.One, Bit.One, Bit.One, Bit.Zero,
                Bit.Zero, Bit.Zero, Bit.One
            };

            Bit[] buffer = new Bit[11];
            buffer[0] = Bit.One;
            buffer[1] = Bit.Zero;
            bits.CopyTo(buffer, 2);

            Assert.Equal(expectedBits, buffer);
        }

        [Fact]
        public void TestCopyToBitsTooSmallBuffer()
        {
            var bitsMock = new Mock<BitSequence>() { CallBase = true };
            bitsMock.Setup(b => b.GetEnumerator()).Returns((new Bit[] { 
                Bit.One, Bit.One, Bit.One, Bit.One,
                Bit.One, Bit.Zero, Bit.Zero, Bit.Zero,
                Bit.One,
            }).AsEnumerable().GetEnumerator());
            var bits = bitsMock.Object;
            
            Bit[] buffer = new Bit[11];

            Assert.Throws<ArgumentException>(() => bits.CopyTo(buffer, 3));
        }

        [Fact]
        public void TestCopyToUnsupportedArrayType()
        {
            var bitsMock = new Mock<BitSequence>() { CallBase = true };

            var buffer = new object[7];
            Assert.Throws<NotSupportedException>(() => bitsMock.Object.CopyTo(buffer, 1));
        }

        [Fact]
        public void TestToBytes()
        {
            var bitsMock = new Mock<BitSequence>() { CallBase = true };
            bitsMock.Setup(b => b.AsByteEnumerable()).Returns(
                new byte[] { 0x2e, 0x12 } // 01110100 01001
            );
            bitsMock.Setup(b => b.Length).Returns(13);
            var bits = bitsMock.Object;

            var expectedBytes = new byte[] { 0x2e, 0x12 };

            var bytes = bits.ToBytes();
            Assert.Equal(expectedBytes, bytes);
        }

        [Fact]
        public void TestToBytesAligned()
        {
            var bitsMock = new Mock<BitSequence>() { CallBase = true };
            bitsMock.Setup(b => b.AsByteEnumerable()).Returns(
                new byte[] { 0b10011011, 0b11100101 }
            );
            bitsMock.Setup(b => b.Length).Returns(16);
            var bits = bitsMock.Object;

            var expectedBytes = new byte[] { 0b10011011, 0b11100101 };

            var bytes = bits.ToBytes();
            Assert.Equal(expectedBytes, bytes);
        }

        [Fact]
        public void TestToBytesOneUnaligned()
        {
            var bitsMock = new Mock<BitSequence>() { CallBase = true };
            bitsMock.Setup(b => b.AsByteEnumerable()).Returns(
                new byte[] { 0b10011011, 0b11100101 }
            );
            bitsMock.Setup(b => b.Length).Returns(9);
            var bits = bitsMock.Object;

            var expectedBytes = new byte[] { 0b10011011, 0b00000001 };

            var bytes = bits.ToBytes();
            Assert.Equal(expectedBytes, bytes);
        }

        [Fact]
        public void TestEnumerator()
        {
            var bitsMock = new Mock<BitSequence>() { CallBase = true };
            bitsMock.Setup(b => b.GetEnumerator()).Returns((new Bit[] {  // 01110100 01001
                Bit.Zero, Bit.One, Bit.One, Bit.One,
                Bit.Zero, Bit.One, Bit.Zero, Bit.Zero,
                Bit.Zero, Bit.One, Bit.Zero, Bit.Zero,
                Bit.One
            }).AsEnumerable().GetEnumerator());
            var bits = bitsMock.Object;

            var expectedBits = new Bit[] {  // 01110100 01001
                Bit.Zero, Bit.One, Bit.One, Bit.One,
                Bit.Zero, Bit.One, Bit.Zero, Bit.Zero,
                Bit.Zero, Bit.One, Bit.Zero, Bit.Zero,
                Bit.One
            };

            foreach ((int i, Bit b) in bits.Enumerate())
            {
                Assert.True(expectedBits[i] == b, $"Expected {expectedBits[i]} but got {b} at position {i}.");
            }
        }
 
        [Fact]
        public void TestToBinaryString()
        {
            var bitsMock = new Mock<BitSequence>() { CallBase = true };
            bitsMock.Setup(b => b.GetEnumerator()).Returns((new Bit[] {  // 01110100 01001
                Bit.Zero, Bit.One, Bit.One, Bit.One,
                Bit.Zero, Bit.One, Bit.Zero, Bit.Zero,
                Bit.Zero, Bit.One, Bit.Zero, Bit.Zero,
                Bit.One
            }).AsEnumerable().GetEnumerator());
            bitsMock.Setup(b => b.Length).Returns(13);
            var bits = bitsMock.Object;

            string expected = "0111010001001";
            string result = bits.ToBinaryString();

            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestToSelectionIndices()
        {
            var bitsMock = new Mock<BitSequence>() { CallBase = true };
            bitsMock.Setup(b => b.GetEnumerator()).Returns((new Bit[] {  // 01110100 01001
                Bit.Zero, Bit.One, Bit.One, Bit.One,
                Bit.Zero, Bit.One, Bit.Zero, Bit.Zero,
                Bit.Zero, Bit.One, Bit.Zero, Bit.Zero,
                Bit.One
            }).AsEnumerable().GetEnumerator());
            bitsMock.Setup(b => b.Length).Returns(13);
            var bits = bitsMock.Object;

            var expected = new int[] { 0, 1, 1, 1, 0, 1, 0, 0, 0, 1, 0, 0, 1 };
            var result = bits.ToSelectionIndices();

            Assert.Equal(expected, result);
        }

        [Fact]
        public void TestConcatenate()
        {
            var leftBitsMock = new Mock<BitSequence>() { CallBase = true };
            leftBitsMock.Setup(b => b.GetEnumerator()).Returns((new Bit[] {  // 01110100 01001
                Bit.Zero, Bit.One, Bit.One, Bit.One,
                Bit.Zero, Bit.One, Bit.Zero, Bit.Zero,
                Bit.Zero, Bit.One, Bit.Zero, Bit.Zero,
                Bit.One
            }).AsEnumerable().GetEnumerator());
            leftBitsMock.Setup(b => b.Length).Returns(13);
            var leftBits = leftBitsMock.Object;

            var rightBitsMock = new Mock<BitSequence>() { CallBase = true };
            rightBitsMock.Setup(b => b.GetEnumerator()).Returns((new Bit[] {  // 01001000 001
                Bit.Zero, Bit.One, Bit.Zero, Bit.Zero,
                Bit.One, Bit.Zero, Bit.Zero, Bit.Zero,
                Bit.Zero, Bit.Zero, Bit.One
            }).AsEnumerable().GetEnumerator());
            rightBitsMock.Setup(b => b.Length).Returns(11);
            var rightBits = rightBitsMock.Object;

            var concatenated = leftBits.Concatenate(rightBits);

            var expectedBits = new Bit[] {
                Bit.Zero, Bit.One, Bit.One, Bit.One,
                Bit.Zero, Bit.One, Bit.Zero, Bit.Zero,
                Bit.Zero, Bit.One, Bit.Zero, Bit.Zero,
                Bit.One, Bit.Zero, Bit.One, Bit.Zero,
                Bit.Zero, Bit.One, Bit.Zero, Bit.Zero,
                Bit.Zero, Bit.Zero, Bit.Zero, Bit.One
            };

            Assert.Equal(expectedBits, concatenated.ToArray());
        }
    
        [Fact]
        public void TestOr()
        {
            var leftBitsMock = new Mock<BitSequence>() { CallBase = true };
            leftBitsMock.Setup(b => b.AsByteEnumerable()).Returns(
                new byte[] { 0x2e, 0x12 } // 01110100 01001
            );
            leftBitsMock.Setup(b => b.Length).Returns(13);
            var leftBits = leftBitsMock.Object;

            var rightBitsMock = new Mock<BitSequence>() { CallBase = true };
            rightBitsMock.Setup(b => b.AsByteEnumerable()).Returns(
                new byte[] { 0x12, 0x14 } // 01001000 00101
            );
            rightBitsMock.Setup(b => b.Length).Returns(13);
            var rightBits = rightBitsMock.Object;

            var expectedBits = new Bit[] { // 01111100 01101
                Bit.Zero, Bit.One, Bit.One, Bit.One,
                Bit.One, Bit.One, Bit.Zero, Bit.Zero,
                Bit.Zero, Bit.One, Bit.One, Bit.Zero,
                Bit.One
            };

            var result = leftBits.Or(rightBits);
            Assert.Equal(expectedBits, result.ToArray());

            var operatorResult = leftBits | rightBits;
            Assert.Equal(expectedBits, operatorResult.ToArray());
        }

        [Fact]
        public void TestXor()
        {
            var leftBitsMock = new Mock<BitSequence>() { CallBase = true };
            leftBitsMock.Setup(b => b.AsByteEnumerable()).Returns(
                new byte[] { 0x2e, 0x12 } // 01110100 01001
            );
            leftBitsMock.Setup(b => b.Length).Returns(13);
            var leftBits = leftBitsMock.Object;

            var rightBitsMock = new Mock<BitSequence>() { CallBase = true };
            rightBitsMock.Setup(b => b.AsByteEnumerable()).Returns(
                new byte[] { 0x12, 0x14 } // 01001000 00101
            );
            rightBitsMock.Setup(b => b.Length).Returns(13);
            var rightBits = rightBitsMock.Object;

            var expectedBits  = new Bit[] { // 0111100 01100
                Bit.Zero, Bit.Zero, Bit.One, Bit.One,
                Bit.One, Bit.One, Bit.Zero, Bit.Zero,
                Bit.Zero, Bit.One, Bit.One, Bit.Zero,
                Bit.Zero
            };
            
            var result = leftBits.Xor(rightBits);
            Assert.Equal(expectedBits, result.ToArray());

            var operatorResult = leftBits ^ rightBits;
            Assert.Equal(expectedBits, operatorResult.ToArray());
        }

        [Fact]
        public void TestAnd()
        {
            var leftBitsMock = new Mock<BitSequence>() { CallBase = true };
            leftBitsMock.Setup(b => b.AsByteEnumerable()).Returns(
                new byte[] { 0x2e, 0x12 } // 01110100 01001
            );
            leftBitsMock.Setup(b => b.Length).Returns(13);
            var leftBits = leftBitsMock.Object;

            var rightBitsMock = new Mock<BitSequence>() { CallBase = true };
            rightBitsMock.Setup(b => b.AsByteEnumerable()).Returns(
                new byte[] { 0x12, 0x14 } // 01001000 00101
            );
            rightBitsMock.Setup(b => b.Length).Returns(13);
            var rightBits = rightBitsMock.Object;

            var expectedBits = new Bit[] { // 01000000 00001
                Bit.Zero, Bit.One, Bit.Zero, Bit.Zero,
                Bit.Zero, Bit.Zero, Bit.Zero, Bit.Zero,
                Bit.Zero, Bit.Zero, Bit.Zero, Bit.Zero,
                Bit.One
            };

            var result = leftBits.And(rightBits);
            Assert.Equal(expectedBits, result.ToArray());

            var operatorResult = leftBits & rightBits;
            Assert.Equal(expectedBits, operatorResult.ToArray());
        }

        [Fact]
        public void TestNot()
        {
            var leftBitsMock = new Mock<BitSequence>() { CallBase = true };
            leftBitsMock.Setup(b => b.AsByteEnumerable()).Returns(
                new byte[] { 0x2e, 0x12 } // 01110100 01001
            );
            leftBitsMock.Setup(b => b.Length).Returns(13);
            var leftBits = leftBitsMock.Object;

            var expectedBits  = new Bit[] { // 10001011 10110
                Bit.One, Bit.Zero, Bit.Zero, Bit.Zero,
                Bit.One, Bit.Zero, Bit.One, Bit.One,
                Bit.One, Bit.Zero, Bit.One, Bit.One,
                Bit.Zero
            };

            var result = leftBits.Not();
            Assert.Equal(expectedBits, result.ToArray());

            var operatorResult = ~leftBits;
            Assert.Equal(expectedBits, operatorResult.ToArray());
        }

        [Fact]
        public void TestOrWithSingleBit()
        {
            var leftBitsMock = new Mock<BitSequence>() { CallBase = true };
            leftBitsMock.Setup(b => b.AsByteEnumerable()).Returns(
                new byte[] { 0x2e, 0x12 } // 01110100 01001
            );
            leftBitsMock.Setup(b => b.Length).Returns(13);
            var leftBits = leftBitsMock.Object;

            var expectedBits = new Bit[] { 
                Bit.One, Bit.One, Bit.One, Bit.One,
                Bit.One, Bit.One, Bit.One, Bit.One,
                Bit.One, Bit.One, Bit.One, Bit.One,
                Bit.One
            };

            var result = leftBits.Or(Bit.One);
            Assert.Equal(expectedBits, result.ToArray());

            var operatorResult = leftBits | Bit.One;
            Assert.Equal(expectedBits, operatorResult.ToArray());
        }

        [Fact]
        public void TestXorWithSingleBit()
        {
            var leftBitsMock = new Mock<BitSequence>() { CallBase = true };
            leftBitsMock.Setup(b => b.AsByteEnumerable()).Returns(
                new byte[] { 0x2e, 0x12 } // 01110100 01001
            );
            leftBitsMock.Setup(b => b.Length).Returns(13);
            var leftBits = leftBitsMock.Object;

            var expectedBits = new Bit[] { 
                Bit.One, Bit.Zero, Bit.Zero, Bit.Zero,
                Bit.One, Bit.Zero, Bit.One, Bit.One,
                Bit.One, Bit.Zero, Bit.One, Bit.One,
                Bit.Zero
            };

            var result = leftBits.Xor(Bit.One);
            Assert.Equal(expectedBits, result.ToArray());

            var operatorResult = leftBits ^ Bit.One;
            Assert.Equal(expectedBits, operatorResult.ToArray());
        }

        [Fact]
        public void TestAndWithSingleBit()
        {
            var leftBitsMock = new Mock<BitSequence>() { CallBase = true };
            leftBitsMock.Setup(b => b.AsByteEnumerable()).Returns(
                new byte[] { 0x2e, 0x12 } // 01110100 01001
            );
            leftBitsMock.Setup(b => b.Length).Returns(13);
            var leftBits = leftBitsMock.Object;

            var expectedBits = new Bit[] { 
                Bit.Zero, Bit.One, Bit.One, Bit.One,
                Bit.Zero, Bit.One, Bit.Zero, Bit.Zero,
                Bit.Zero, Bit.One, Bit.Zero, Bit.Zero,
                Bit.One
            };

            var result = leftBits.And(Bit.One);
            Assert.Equal(expectedBits, result.ToArray());

            var operatorResult = leftBits & Bit.One;
            Assert.Equal(expectedBits, operatorResult.ToArray());
        }

 
    }
}