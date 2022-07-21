using Xunit;
using System.Collections.Generic;

namespace CompactOT.DataStructures
{
    public class BitToByteEnumeratorTests
    {
        [Fact]
        public void TestWithNonByteBoundary()
        {
            Bit[] bits = new Bit[] {
                Bit.Zero,  Bit.One, Bit.Zero, Bit.Zero,  Bit.One, Bit.Zero, Bit.Zero, Bit.Zero,
                 Bit.One,  Bit.One, Bit.Zero, Bit.Zero, Bit.Zero
            };
            var bitEnumerator = ((IEnumerable<Bit>)bits).GetEnumerator();

            var enumerator = new BitToByteEnumerator(bitEnumerator);

            byte[] expectedBytes = new byte[] { 0x12, 0x03 };
            var expectedByteEnumerator = expectedBytes.GetEnumerator();

            while (expectedByteEnumerator.MoveNext())
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal(expectedByteEnumerator.Current, enumerator.Current);
            }

            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void TestWithByteBoundary()
        {
            Bit[] bits = new Bit[] {
                Bit.Zero,  Bit.One, Bit.Zero, Bit.Zero,  Bit.One, Bit.Zero, Bit.Zero, Bit.Zero,
                 Bit.One,  Bit.One, Bit.Zero, Bit.Zero, Bit.Zero, Bit.Zero,  Bit.One, Bit.Zero
            };
            var bitEnumerator = ((IEnumerable<Bit>)bits).GetEnumerator();

            var enumerator = new BitToByteEnumerator(bitEnumerator);

            byte[] expectedBytes = new byte[] { 0x12, 0x43 };
            var expectedByteEnumerator = expectedBytes.GetEnumerator();

            while (expectedByteEnumerator.MoveNext())
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal(expectedByteEnumerator.Current, enumerator.Current);
            }

            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void TestReset()
        {
            Bit[] bits = new Bit[] {
                Bit.Zero,  Bit.One, Bit.Zero, Bit.Zero,  Bit.One, Bit.Zero, Bit.Zero, Bit.Zero,
                 Bit.One
            };
            var bitEnumerator = ((IEnumerable<Bit>)bits).GetEnumerator();

            var enumerator = new BitToByteEnumerator(bitEnumerator);

            byte[] expectedBytes = new byte[] { 0x12, 0x01 };
            var expectedByteEnumerator = expectedBytes.GetEnumerator();

            while (enumerator.MoveNext()) { }
            enumerator.Reset();

            while (expectedByteEnumerator.MoveNext())
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal(expectedByteEnumerator.Current, enumerator.Current);
            }
            Assert.False(enumerator.MoveNext());
        }

    }
}