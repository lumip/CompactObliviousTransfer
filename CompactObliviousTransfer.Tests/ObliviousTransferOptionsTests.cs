// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Security.Cryptography;

using Moq;
using Xunit;

namespace CompactOT.DataStructures
{
    public class ObliviousTransferOptionsTests
    {

        const int NumberOfInvocations = 3;
        const int NumberOfOptions = 4;
        const int NumberOfMessageBits = 5;

        byte[] Bytes;
        ObliviousTransferOptions Options;
        byte[][][] ExpectedOptions;

        public ObliviousTransferOptionsTests()
        {
            // 11110 00010 00011 11011
            // 01000 01000 11110 11010
            // 01100 10111 10111 11001
            Bytes = new byte[] { 0x0f, 0xe1, 0x2d, 0xc4, 0x5b, 0xa6, 0xf7, 0x89 };
            Options = ObliviousTransferOptions.FromBitArray(
                new EnumeratedBitArrayView(Bytes, NumberOfInvocations * NumberOfOptions * NumberOfMessageBits),
                NumberOfInvocations, NumberOfOptions, NumberOfMessageBits
            );
            ExpectedOptions = new byte[][][] {
                new byte[][] { new byte[] { 0b01111 }, new byte[] { 0b01000 }, new byte[] { 0b11000 }, new byte[] { 0b11011 } },
                new byte[][] { new byte[] { 0b00010 }, new byte[] { 0b00010 }, new byte[] { 0b01111 }, new byte[] { 0b01011 } },
                new byte[][] { new byte[] { 0b00110 }, new byte[] { 0b11101 }, new byte[] { 0b11101 }, new byte[] { 0b10011 } }
            };
        }

        [Fact]
        public void TestConstruction()
        {
            var options = new ObliviousTransferOptions(NumberOfInvocations, NumberOfOptions, NumberOfMessageBits);
            Assert.Equal(NumberOfInvocations, options.NumberOfInvocations);
            Assert.Equal(NumberOfOptions, options.NumberOfOptions);
            Assert.Equal(NumberOfMessageBits, options.NumberOfMessageBits);
        }

        [Fact]
        public void TestFromBitArray()
        {
            int invocationLength = NumberOfOptions * NumberOfMessageBits;

            var firstInvocationBits = Options.GetInvocation(0);
            var expectedFirstInvocationBits = BitArray.FromBytes(new byte[] { 0x0f, 0xe1, 0x0d }, invocationLength);
            Assert.Equal(expectedFirstInvocationBits, firstInvocationBits);

            var secondInvocationBits = Options.GetInvocation(1);
            var expectedSecondInvocationBits = BitArray.FromBytes(new byte[] { 0x42, 0xbc, 0x05 }, invocationLength);
            Assert.Equal(expectedSecondInvocationBits, secondInvocationBits);

            var thirdInvocationBits = Options.GetInvocation(2);
            var expectedThirdInvocationBits = BitArray.FromBytes(new byte[] { 0xa6, 0xf7, 0x09 }, invocationLength);
            Assert.Equal(expectedThirdInvocationBits, thirdInvocationBits);
        }

        [Fact]
        public void TestFromBitArrayBadLength()
        {
            Assert.Throws<ArgumentException>(
                () => ObliviousTransferOptions.FromBitArray(BitArray.Empty, 2, 3, 1)
            );
        }

        [Fact]
        public void TestCreateLike()
        {
            var template = new ObliviousTransferOptions(NumberOfInvocations, NumberOfOptions, NumberOfMessageBits);
            var options = ObliviousTransferOptions.CreateLike(template);
            Assert.Equal(template.NumberOfInvocations, options.NumberOfInvocations);
            Assert.Equal(template.NumberOfOptions, options.NumberOfOptions);
            Assert.Equal(template.NumberOfMessageBits, options.NumberOfMessageBits);
        }

        [Fact]
        public void TestCreateRandom()
        {
            byte[] randomBytes = new byte[Bytes.Length];
            for (int i = 0; i < randomBytes.Length; ++i)
            {
                randomBytes[i] = (byte)(Bytes[i] ^ 0xff);
            }

            var rngMock = new Mock<RandomNumberGenerator>();
            rngMock.Setup(rng => rng.GetBytes(It.IsAny<byte[]>()))
                   .Callback((byte[] bytes) => Array.Copy(randomBytes, bytes, bytes.Length));

            var options = ObliviousTransferOptions.CreateRandom(
                NumberOfInvocations, NumberOfOptions, NumberOfMessageBits, rngMock.Object
            );

            int invocationLength = NumberOfOptions * NumberOfMessageBits;

            var firstInvocationBits = options.GetInvocation(0);
            var expectedFirstInvocationBits = BitArray.FromBytes(new byte[] { 0x0f ^ 0xff, 0xe1 ^ 0xff, 0x0d ^ 0x0f }, invocationLength);
            Assert.Equal(expectedFirstInvocationBits, firstInvocationBits);

            var secondInvocationBits = options.GetInvocation(1);
            var expectedSecondInvocationBits = BitArray.FromBytes(new byte[] { 0x42 ^ 0xff, 0xbc ^ 0xff, 0x05 ^ 0x0f }, invocationLength);
            Assert.Equal(expectedSecondInvocationBits, secondInvocationBits);

            var thirdInvocationBits = options.GetInvocation(2);
            var expectedThirdInvocationBits = BitArray.FromBytes(new byte[] { 0xa6 ^ 0xff, 0xf7 ^ 0xff, 0x09 ^ 0x0f }, invocationLength);
            Assert.Equal(expectedThirdInvocationBits, thirdInvocationBits);
        }

        [Fact]
        public void TestFromCorrelatedTransfer()
        {
            int numberOfInvocations = 4;
            int numberOfOptions = 3;
            int numberOfMessageBits = 5;

            BitMatrix firstOptions = new BitMatrix(numberOfInvocations, numberOfMessageBits,
                BitArray.FromBinaryString("10101 01010 11100 00011")
            );

            ObliviousTransferOptions correlations = ObliviousTransferOptions.FromBitArray(
                BitArray.FromBinaryString("00000 11111 11100 00011" +
                                          "11111 00000 00011 11100"),
                numberOfInvocations, numberOfOptions - 1, numberOfMessageBits
            );

            var options = ObliviousTransferOptions.FromCorrelatedTransfer(firstOptions, correlations);

            Assert.Equal(numberOfInvocations, options.NumberOfInvocations);
            Assert.Equal(numberOfOptions, options.NumberOfOptions);
            Assert.Equal(numberOfMessageBits, options.NumberOfMessageBits);

            for (int i = 0; i < numberOfInvocations; i++)
            {
                var expectedFirstMessageBits = firstOptions.GetRow(i);
                var messageBits = options.GetMessage(i, 0);
                Assert.Equal(expectedFirstMessageBits, messageBits);

                for (int j = 1; j < numberOfOptions; j++)
                {
                    var expectedMessageBits = expectedFirstMessageBits ^ correlations.GetMessage(i, j - 1);
                    messageBits = options.GetMessage(i, j);
                    Assert.Equal(expectedMessageBits, messageBits);
                }
            }
        }

        [Fact]
        public void TestFromCorrelatedTransferDifferentNumberOfInvocations()
        {
            var firstOptions = new BitMatrix(3, 2);
            var correlations = new ObliviousTransferOptions(2, 4, 2);

            Assert.Throws<ArgumentException>(
                () => ObliviousTransferOptions.FromCorrelatedTransfer(firstOptions, correlations)
            );
        }

        [Fact]
        public void TestFromCorrelatedTransferDifferentNumberOfMessageBits()
        {
            var firstOptions = new BitMatrix(3, 17);
            var correlations = new ObliviousTransferOptions(3, 4, 15);

            Assert.Throws<ArgumentException>(
                () => ObliviousTransferOptions.FromCorrelatedTransfer(firstOptions, correlations)
            );
        }

        [Fact]
        public void TestSetAndGetInvocationWithBitSequence()
        {
            int invocationLength = NumberOfOptions * NumberOfMessageBits;

            var invocationBits = Options.GetInvocation(1);
            var expectedInvocationBits = BitArray.FromBytes(new byte[] { 0x42, 0xbc, 0x05 }, invocationLength);
            Assert.Equal(expectedInvocationBits, invocationBits);

            var newInvocationBits = BitArray.FromBytes(new byte[] { 0x10, 0x32, 0x54 }, invocationLength);
            Options.SetInvocation(1, newInvocationBits);
            invocationBits = Options.GetInvocation(1);
            Assert.Equal(invocationBits, newInvocationBits);
        }

        [Fact]
        public void TestSetInvocationWithBitSequenceBadLength()
        {
            int invocationLength = NumberOfOptions * NumberOfMessageBits;

            var newInvocationBits = ConstantBitArrayView.MakeZeros(invocationLength + 2);

            Assert.Throws<ArgumentException>(
                () => Options.SetInvocation(1, newInvocationBits)
            );
        }

        [Fact]
        public void TestSetInvocationWithBitSequenceOutOfRange()
        {
            int invocationLength = NumberOfOptions * NumberOfMessageBits;

            var newInvocationBits = BitArray.FromBytes(new byte[] { 0x10, 0x32, 0x54 }, invocationLength);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Options.SetInvocation(NumberOfInvocations, newInvocationBits)
            );
        }

        [Fact]
        public void TestSetAndGetInvocationWithBitMatrix()
        {
            int invocationLength = NumberOfOptions * NumberOfMessageBits;

            var invocationBits = Options.GetInvocation(1);
            var expectedInvocationBits = BitArray.FromBytes(new byte[] { 0x42, 0xbc, 0x05 }, invocationLength);
            Assert.Equal(expectedInvocationBits, invocationBits);

            var newInvocationBits = BitArray.FromBytes(new byte[] { 0x10, 0x32, 0x54 }, invocationLength);
            var newInvocationMatrix = new BitMatrix(NumberOfOptions, NumberOfMessageBits, newInvocationBits);

            Options.SetInvocation(1, newInvocationMatrix);
            invocationBits = Options.GetInvocation(1);
            Assert.Equal(invocationBits, newInvocationBits);
        }

        [Theory]
        [InlineData(NumberOfOptions - 1, NumberOfMessageBits)]
        [InlineData(NumberOfOptions, NumberOfMessageBits + 1)]
        public void TestSetInvocationWithBitMatrixBadSize(int rows, int cols)
        {
            var newInvocationMatrix = new BitMatrix(rows, cols);
            Assert.Throws<ArgumentException>(
                () => Options.SetInvocation(1, newInvocationMatrix)
            );
        }

        [Fact]
        public void TestGetAndSetInvocationWithBitSequenceArray()
        {
            int invocationLength = NumberOfOptions * NumberOfMessageBits;

            var newInvocationBits = BitArray.FromBytes(new byte[] { 0x10, 0x32, 0x54 }, invocationLength);
            var newInvocationArray = new BitSequence[NumberOfOptions];
            for (int j = 0; j < NumberOfOptions; j++)
            {
                newInvocationArray[j] = new BitArraySlice(newInvocationBits, j * NumberOfMessageBits, (j + 1) * NumberOfMessageBits);
            }

            Options.SetInvocation(1, newInvocationArray);
            var invocationBits = Options.GetInvocation(1);
            Assert.Equal(invocationBits, newInvocationBits);
        }

        [Fact]
        public void TestSetInvocationWithBitSequenceArrayBadLength()
        {
            int invocationLength = NumberOfOptions * NumberOfMessageBits;

            var newInvocationBits = BitArray.FromBytes(new byte[] { 0x10, 0x32, 0x54 }, invocationLength);
            var newInvocationArray = new BitSequence[NumberOfOptions - 1];
            for (int j = 0; j < NumberOfOptions - 1; j++)
            {
                newInvocationArray[j] = new BitArraySlice(newInvocationBits, j * NumberOfMessageBits, (j + 1) * NumberOfMessageBits);
            }

            Assert.Throws<ArgumentException>(
                () => Options.SetInvocation(1, newInvocationArray)
            );
        }

        [Fact]
        public void TestGetAndSetInvocationWithByteArray()
        {
            int numberOfMessageBits = 16;
            var options = new ObliviousTransferOptions(NumberOfInvocations, NumberOfOptions, numberOfMessageBits);

            byte[][] newMessageBytes = new byte[][] {
                new byte[] { 0, 1 },
                new byte[] { 2, 3 },
                new byte[] { 4, 5 },
                new byte[] { 6, 7 },
            };

            var expectedInvocationBits = BitArray.FromBytes(newMessageBytes.Flatten(), 64);

            options.SetInvocation(0, newMessageBytes);
            var invocationBits = options.GetInvocation(0);

            Assert.Equal(expectedInvocationBits, invocationBits);
        }

        [Fact]
        public void TestSetInvocationWithByteArrayBadLength()
        {
            int numberOfMessageBits = 16;
            var options = new ObliviousTransferOptions(NumberOfInvocations, NumberOfOptions, numberOfMessageBits);

            byte[][] newMessageBytes = new byte[][] {
                new byte[] { 0, 1 },
                new byte[] { 2, 3 },
                new byte[] { 4, 5 },
                new byte[] { 6, 7 },
                new byte[] { 8, 9 },
            };

            Assert.Throws<ArgumentException>(
                () => options.SetInvocation(0, newMessageBytes)
            );
        }

        [Fact]
        public void TestSetInvocationWithByteArrayForIncompatibleMessageLength()
        {
            byte[][] newMessageBytes = new byte[][] {
                new byte[] { 0, 1 },
                new byte[] { 2, 3 },
                new byte[] { 4, 5 },
                new byte[] { 6, 7 },
            };

            Assert.Throws<NotSupportedException>(
                () => Options.SetInvocation(0, newMessageBytes)
            );
        }


        [Fact]
        public void TestGetOptions()
        {
            var optionsBits = Options.GetOptions(1);
            var expectedOptionsBits = BitArray.FromBinaryString("00010 01000 10111");
            var expectedOptionsMatrix = new BitMatrix(NumberOfInvocations, NumberOfMessageBits, expectedOptionsBits);
            for (int i = 0; i < NumberOfInvocations; i++)
            {
                expectedOptionsMatrix.SetRow(i, BitArray.FromBytes(ExpectedOptions[i][1], NumberOfMessageBits));
            }
            Assert.Equal(expectedOptionsMatrix, optionsBits);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(NumberOfOptions + 1)]
        public void TestGetOptionsOutOfRange(int optionsIndex)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Options.GetOptions(optionsIndex)
            );
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(0, 3)]
        [InlineData(1, 0)]
        [InlineData(1, 3)]
        [InlineData(2, 0)]
        [InlineData(2, 3)]
        public void TestGetAndSetMessage(int invocationIndex, int optionIndex)
        {
            byte newMessageByte = 0x1f;

            ExpectedOptions[invocationIndex][optionIndex][0] = newMessageByte;

            var newMessage = BitArray.FromBytes(ExpectedOptions[invocationIndex][optionIndex], NumberOfMessageBits);

            Options.SetMessage(invocationIndex, optionIndex, newMessage);

            for (int i = 0; i < NumberOfInvocations; ++i)
            {
                for (int j = 0; j < NumberOfOptions; ++j)
                {
                    var expectedMessageBits = BitArray.FromBytes(ExpectedOptions[i][j], NumberOfMessageBits);
                    Assert.Equal(expectedMessageBits, Options.GetMessage(i, j));
                }
            }
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(NumberOfInvocations + 1, 0)]
        [InlineData(0, -1)]
        [InlineData(0, NumberOfOptions + 1)]
        public void TestGetMessageOutOfRange(int invocationIndex, int optionIndex)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Options.GetMessage(invocationIndex, optionIndex)
            );
        }

        [Fact]
        public void TestSetMessageBadLength()
        {
            var newMessageBits = BitSequence.Empty;
            Assert.Throws<ArgumentException>(
                () => Options.SetMessage(0, 0, newMessageBits)
            );
        }

        [Theory]
        [InlineData(NumberOfInvocations - 1, NumberOfOptions, NumberOfMessageBits)]
        [InlineData(NumberOfInvocations, NumberOfOptions - 1, NumberOfMessageBits)]
        [InlineData(NumberOfInvocations, NumberOfOptions, NumberOfMessageBits - 1)]
        public void TestEqualsWithShapeMismatch(int numberOfInvocations, int numberOfOptions, int numberOfMessageBits)
        {
            var other = new ObliviousTransferOptions(numberOfInvocations, numberOfOptions, numberOfMessageBits);
            Assert.False(Options.Equals(other));
        }

        [Fact]
        public void TestEqualsWithOtherObjects()
        {
            Assert.False(Options.Equals(new object()));
            Assert.False(Options.Equals(null));
        }

        [Fact]
        public void TestEqualsWhenShapeCorrect()
        {
            var other = ObliviousTransferOptions.CreateLike(Options);
            Assert.False(Options.Equals(other));
            Assert.NotEqual(Options.GetHashCode(), other.GetHashCode());

            for (int i = 0; i < NumberOfInvocations; i++)
            {
                other.SetInvocation(i, Options.GetInvocation(i));
            }
            Assert.True(Options.Equals(other));
            Assert.Equal(Options.GetHashCode(), other.GetHashCode());
        }
    }
}
