using System;
using System.Linq;
using System.Security.Cryptography;
using Moq;

using Xunit;

namespace CompactOT.DataStructures
{
    public class ObliviousTransferOptionsTests : IDisposable
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

        public void Dispose()
        {

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
        public void TestCreateRandom()
        {
            byte[] randomBytes = new byte[Bytes.Length];
            for (int i = 0; i < randomBytes.Length; ++i) randomBytes[i] = (byte)(Bytes[i] ^ 0xff);

            var rngMock = new Mock<RandomNumberGenerator>();
            rngMock.Setup(rng => rng.GetBytes(It.IsAny<byte[]>()))
                   .Callback((byte[] bytes) => Array.Copy(randomBytes, bytes, bytes.Length));

            var options = ObliviousTransferOptions.CreateRandom(
                NumberOfInvocations, NumberOfOptions, NumberOfMessageBits, rngMock.Object
            );

            int invocationLength = NumberOfOptions * NumberOfMessageBits;

            var firstInvocationBits = options.GetInvocation(0);
            var expectedFirstInvocationBits = BitArray.FromBytes(new byte[] { 0x0f ^ 0xff , 0xe1 ^ 0xff, 0x0d ^ 0x0f }, invocationLength);
            Assert.Equal(expectedFirstInvocationBits, firstInvocationBits);

            var secondInvocationBits = options.GetInvocation(1);
            var expectedSecondInvocationBits = BitArray.FromBytes(new byte[] { 0x42 ^ 0xff, 0xbc ^ 0xff, 0x05 ^ 0x0f }, invocationLength);
            Assert.Equal(expectedSecondInvocationBits, secondInvocationBits);

            var thirdInvocationBits = options.GetInvocation(2);
            var expectedThirdInvocationBits = BitArray.FromBytes(new byte[] { 0xa6 ^ 0xff, 0xf7 ^ 0xff, 0x09 ^ 0x0f }, invocationLength);
            Assert.Equal(expectedThirdInvocationBits, thirdInvocationBits);
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
        public void TestSetAndGetInvocation()
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
    }
}