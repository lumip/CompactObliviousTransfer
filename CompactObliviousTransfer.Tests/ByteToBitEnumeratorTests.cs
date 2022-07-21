using Xunit;
using System.Collections.Generic;

namespace CompactOT.DataStructures
{
    public class ByteToBitEnumeratorTests
    {
        [Fact]
        public void TestWithLength()
        {
            byte[] bytes = new byte[] { 0x12, 0x43 };
            var enumerator = new ByteToBitEnumerator(((IEnumerable<byte>)bytes).GetEnumerator(), 8 + 5);


            Bit[] bits = new Bit[] {
                Bit.Zero,  Bit.One, Bit.Zero, Bit.Zero,  Bit.One, Bit.Zero, Bit.Zero, Bit.Zero,
                 Bit.One,  Bit.One, Bit.Zero, Bit.Zero, Bit.Zero
            };
            var bitEnumerator = bits.GetEnumerator();

            while (bitEnumerator.MoveNext())
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal(bitEnumerator.Current, enumerator.Current);
            }

            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void TestWithoutLength()
        {
            byte[] bytes = new byte[] { 0x12, 0x43 };
            var enumerator = new ByteToBitEnumerator(((IEnumerable<byte>)bytes).GetEnumerator());


            Bit[] bits = new Bit[] {
                Bit.Zero,  Bit.One, Bit.Zero, Bit.Zero,  Bit.One, Bit.Zero, Bit.Zero, Bit.Zero,
                 Bit.One,  Bit.One, Bit.Zero, Bit.Zero, Bit.Zero, Bit.Zero,  Bit.One, Bit.Zero
            };
            var bitEnumerator = bits.GetEnumerator();

            while (bitEnumerator.MoveNext())
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal(bitEnumerator.Current, enumerator.Current);
            }

            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void TestReset()
        {
            byte[] bytes = new byte[] { 0x12, 0x43 };
            var enumerator = new ByteToBitEnumerator(((IEnumerable<byte>)bytes).GetEnumerator(), 9);


            Bit[] bits = new Bit[] {
                Bit.Zero,  Bit.One, Bit.Zero, Bit.Zero,  Bit.One, Bit.Zero, Bit.Zero, Bit.Zero,
                 Bit.One
            };
            var bitEnumerator = bits.GetEnumerator();

            while (enumerator.MoveNext()) { }
            enumerator.Reset();

            while (bitEnumerator.MoveNext())
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal(bitEnumerator.Current, enumerator.Current);
            }

            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void TestBaseEnumeratorTooShort()
        {
            byte[] bytes = new byte[] { 0x12, 0x43 };
            var enumerator = new ByteToBitEnumerator(((IEnumerable<byte>)bytes).GetEnumerator(), 20);

            Assert.Throws(typeof(BaseEnumeratorExhaustedException), () => {
                while (enumerator.MoveNext()) { }
            });
        }
    }
}