using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;

namespace CompactOT.DataStructures
{
    public class EnumeratedBitArrayViewTests
    {
        [Fact]
        public void TestConstruction()
        {
            byte[] bytes = new byte[] { 0x56, 0x8d, 0xa3 };
            var bits = new EnumeratedBitArrayView(bytes, 11);
            Assert.Equal(11, bits.Length);

            var expectedBits = BitArray.FromBinaryString("01101010 101");
            Assert.Equal(bits, expectedBits);
        }

        [Fact]
        public void TestAsByteEnumerable()
        {
            byte[] bytes = new byte[] { 0x56, 0x8d, 0xa3 };
            var bits = new EnumeratedBitArrayView(bytes, 11);

            var bitsAsArray = bits.AsByteEnumerable().ToArray();
            var expectedBitsAsArray = new byte[] { 0x56, 0x05 };
            // todo: currently fails, last byte is not masked properly. think whether that is relevant requirement as that usually gets ignored anyways..
            Assert.Equal(expectedBitsAsArray, bitsAsArray);
        }
    }
}