using Xunit;
using System;

using CompactOT.DataStructures;

namespace CompactOT.Codes
{
    public class WalshHadamardCodeTests
    {

        [Fact]
        public void TestGetParity()
        {
            Assert.Equal(0, WalshHadamardCode.GetParity(0b101));
            Assert.Equal(1, WalshHadamardCode.GetParity(0b1110011));
            Assert.Equal(0, WalshHadamardCode.GetParity(((int)0x59959C17)));
            Assert.Equal(1, WalshHadamardCode.GetParity(((int)0x19959C17)));
        }

        [Fact]
        public void TestComputeWalshHadamardCode()
        {
            var code = new WalshHadamardCode(8);
            var result = code.Encode(0b101);

            var expected = BitArray.FromBinaryString("01011010");
            Assert.Equal(expected, result);
        } 


        [Fact]
        public void TestComputeWalshHadamardCodeUpperBound()
        {
            var code = new WalshHadamardCode(8);
            var result = code.Encode(0b111);

            var expected = BitArray.FromBinaryString("01101001");
            Assert.Equal(expected, result);
        } 


        [Fact]
        public void TestComputeWalshHadamardCodeInvalidCodeLength()
        {
            Assert.Throws<ArgumentException>(() => new WalshHadamardCode(3));
        }

        [Fact]
        public void TestComputeWalshHadamardCodeTooLargeValue()
        {
            var code = new WalshHadamardCode(4);
            Assert.Throws<ArgumentOutOfRangeException>(() => code.Encode(0b10000));
        }

        [Fact]
        public void TestCodeLength()
        {
            int codeLength = 6;
            var code = new WalshHadamardCode(codeLength);
            Assert.Equal(codeLength, code.CodeLength);
        }

    }
}