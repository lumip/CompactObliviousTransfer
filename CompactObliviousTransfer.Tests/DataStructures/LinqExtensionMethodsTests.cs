// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Linq;

using Xunit;

namespace CompactOT.DataStructures
{
    public class LinqExtensionMethodsTests
    {
        [Fact]
        public void TestWriteInto()
        {
            var sourceArray = new byte[] { 0x01, 0x23, 0x45 };
            var buffer = new byte[5] { 0, 0, 0, 0, 0 };
            var expected = new byte[5] { 0x00, 0x01, 0x23, 0x45, 0x00 };

            sourceArray.AsEnumerable().WriteInto(buffer, 1);

            Assert.Equal(expected, buffer);
        }

        [Fact]
        public void TestWriteIntoTooSmallBuffer()
        {
            var sourceArray = new byte[] { 0x01, 0x23, 0x45 };
            var buffer = new byte[5] { 0, 0, 0, 0, 0 };

            Assert.Throws<ArgumentException>(() => sourceArray.WriteInto(buffer, 3));

        }

        [Fact]
        public void TestEnumerate()
        {
            var sourceArray = new string[] { "first", "second", "third", "fourth" };

            foreach ((int index, string value) in sourceArray.Enumerate())
            {
                Assert.Equal(sourceArray[index], value);
            }
        }

        [Fact]
        public void TestTile()
        {
            var sourceArray = new string[] { "first", "second", "third", "fourth" };
            int numberOfRepeats = 3;
            var tiledArray = sourceArray.Tile(numberOfRepeats).ToArray();

            Assert.Equal(numberOfRepeats * sourceArray.Length, tiledArray.Length);

            for (int i = 0; i < numberOfRepeats; i++)
            {
                for (int j = 0; j < sourceArray.Length; j++)
                {
                    Assert.Equal(sourceArray[j], tiledArray[i * sourceArray.Length + j]);
                }
            }
        }

        [Fact]
        public void TestFlatten()
        {
            var sourceArray = new string[][]
            {
                new string[] { "first", "second", "third", "fourth" },
                new string[] { "fifth", "sixth"  }
            };

            var expectedArray = new string[] { "first", "second", "third", "fourth", "fifth", "sixth" };

            var flattenedArray = sourceArray.Flatten();

            Assert.Equal(expectedArray, flattenedArray);
        }

    }
}
