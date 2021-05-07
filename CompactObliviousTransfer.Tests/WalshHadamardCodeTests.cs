using Xunit;
using System;

using CompactOT.DataStructures;
namespace CompactOT
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
            var expected = BitArray.FromBinaryString("01011010");
            var result = WalshHadamardCode.ComputeWalshHadamardCode(0b101, 8);
            Assert.Equal(expected, result);
        } 

        [Fact]
        public void TestComputeWalshHadamardCodeInvalidCodeLength()
        {
            Assert.Throws<ArgumentException>(() => WalshHadamardCode.ComputeWalshHadamardCode(0b111, 3));
        }

    }
}