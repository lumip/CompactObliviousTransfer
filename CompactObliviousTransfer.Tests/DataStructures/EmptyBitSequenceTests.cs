// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;

using Xunit;
using Moq;
using System.Runtime.CompilerServices;

namespace CompactOT.DataStructures
{
    public class EmptyBitSequenceTests
    {

        [Fact]
        public void TestProperties() 
        {
            var emptySequence = new EmptyBitSequence();

            Assert.Equal(0, emptySequence.Length);
            Assert.True(emptySequence.IsSynchronized);
            Assert.Same(emptySequence, emptySequence.SyncRoot);
            Assert.True(emptySequence.IsReadOnly);
        }

        [Fact]
        public void TestAsBytesEnumerable()
        {
            var enumerable = new EmptyBitSequence().AsByteEnumerable();

            Assert.Empty(enumerable);
        }

        [Fact]
        public void TestGetEnumerator()
        {
            var enumerator = new EmptyBitSequence().GetEnumerator();

            Assert.False(enumerator.MoveNext());
        }

    }
}