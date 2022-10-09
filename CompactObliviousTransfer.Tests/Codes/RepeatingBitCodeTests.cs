using Xunit;
using System;

using CompactOT.DataStructures;

namespace CompactOT.Codes
{
    public class RepeatingBitCodeTests
    {

        [Fact]
        public void TestEncodeOne()
        {
            var code = new RepeatingBitCode(8);
            var result = code.Encode(1);

            var expected = BitArray.FromBinaryString("11111111");
            Assert.Equal(expected, result);
        } 


        [Fact]
        public void TestEncodeZero()
        {
            var code = new RepeatingBitCode(8);
            var result = code.Encode(0);

            var expected = BitArray.FromBinaryString("00000000");
            Assert.Equal(expected, result);
        } 

        [Fact]
        public void TestEncodeRejectsTooLargeValue()
        {
            var code = new RepeatingBitCode(4);
            Assert.Throws<ArgumentOutOfRangeException>(() => code.Encode(2));
        }

        [Fact]
        public void TestCreateWithDistance()
        {
            int distance = 7;
            var code = RepeatingBitCode.CreateWithDistance(distance);
            int expectedDistance = 7;
            int expectedCodeLength = 7;
            int expectedMaximumMessage = 1;
            Assert.Equal(expectedDistance, code.Distance);
            Assert.Equal(expectedCodeLength, code.CodeLength);
            Assert.Equal(expectedMaximumMessage, code.MaximumMessage);
        }

    }
}
