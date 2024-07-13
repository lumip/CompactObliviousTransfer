// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections;
using System.Collections.Generic;

using Moq;
using Xunit;

namespace CompactOT.DataStructures
{
    public class BaseEnumerableTests
    {
        [Fact]
        public void TestGetEnumeratorClassic()
        {
            var enumerableMock = new Mock<BaseEnumerable<int>>(MockBehavior.Strict) { CallBase = false };
            var enumeratorMock = new Mock<IEnumerator<int>>();

            enumerableMock.Setup<IEnumerator<int>>(e => e.GetEnumerator()).Returns(enumeratorMock.Object);

            var enumerable = enumerableMock.Object;

            var result = ((IEnumerable)enumerable).GetEnumerator();

            Assert.Same(enumeratorMock.Object, result);

            enumerableMock.Verify<IEnumerator<int>>(e => e.GetEnumerator(), Times.Once);
        }
    }
}
