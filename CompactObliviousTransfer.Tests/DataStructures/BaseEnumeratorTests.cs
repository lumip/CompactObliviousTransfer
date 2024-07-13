// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections;

using Moq;
using Xunit;

namespace CompactOT.DataStructures
{
    public class BaseEnumeratorTests
    {
        [Fact]
        public void TestGetCurrentClassic()
        {
            int expected = 17;

            var enumeratorMock = new Mock<BaseEnumerator<int>>(MockBehavior.Strict) { CallBase = true };
            enumeratorMock.Setup<int>(e => e.Current).Returns(expected);

            var enumerator = enumeratorMock.Object;

            var result = ((IEnumerator)enumerator).Current;

            Assert.Equal(expected, result);

            enumeratorMock.Verify<int>(e => e.Current, Times.Once);
        }
    }
}
