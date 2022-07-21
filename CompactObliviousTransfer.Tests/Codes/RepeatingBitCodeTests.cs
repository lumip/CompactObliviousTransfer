using Xunit;
using System;

using CompactOT.DataStructures;

namespace CompactOT.Codes
{
    public class RepeatingBitCodeTests
    {

        [Fact]
        public void TestComputeCodeOnes()
        {
            var code = new RepeatingBitCode(8);
            var result = code.Encode(1);

            var expected = BitArray.FromBinaryString("11111111");
            Assert.Equal(expected, result);
        } 


        [Fact]
        public void TestComputeCodeZeros()
        {
            var code = new RepeatingBitCode(8);
            var result = code.Encode(0);

            var expected = BitArray.FromBinaryString("00000000");
            Assert.Equal(expected, result);
        } 

        [Fact]
        public void TestComputeCodeTooLargeValue()
        {
            var code = new RepeatingBitCode(4);
            Assert.Throws<ArgumentOutOfRangeException>(() => code.Encode(2));
        }

        [Fact]
        public void TestCodeLength()
        {
            int codeLength = 6;
            var code = new RepeatingBitCode(codeLength);
            Assert.Equal(codeLength, code.CodeLength);
        }

    }
}
