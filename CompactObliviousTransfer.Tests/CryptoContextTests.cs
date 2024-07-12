// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using Xunit;
using Moq;
using System;
using System.Security.Cryptography;

namespace CompactOT
{
    public class CryptoContextTests
    {

        [Fact]
        public void TestConstructorAndProperties()
        {
            var randomNumberGenerator = RandomNumberGenerator.Create();
            var hashAlgorithmProvider = new SHA256Provider();

            var cryptoContext = new CryptoContext(randomNumberGenerator, hashAlgorithmProvider);

            Assert.Same(randomNumberGenerator, cryptoContext.RandomNumberGenerator);
            Assert.Same(hashAlgorithmProvider, cryptoContext.HashAlgorithmProvider);
        }

        [Fact]
        public void TestSecurityLevel()
        {
            var expected = 256;

            var hashAlgorithmProviderMock = new Mock<HashAlgorithmProvider>();
            hashAlgorithmProviderMock.Setup(hap => hap.SecurityLevel).Returns(expected);
            var hashAlgorithmProvider = hashAlgorithmProviderMock.Object;

            var cryptoContext = new CryptoContext(RandomNumberGenerator.Create(), hashAlgorithmProvider);
            var securityLevel = cryptoContext.SecurityLevel;

            Assert.Equal(expected, securityLevel);
            hashAlgorithmProviderMock.Verify(hap => hap.SecurityLevel, Times.Once);
        }

        [Theory]
        [InlineData(100, 128)]
        [InlineData(128, 128)]
        [InlineData(256, 256)]
        public void TestCreateWithSecurityLevel(int requestedSecurityLevel, int expectedSecurityLevel)
        {
            var cryptoContext = CryptoContext.CreateWithSecurityLevel(requestedSecurityLevel);
            Assert.Equal(expectedSecurityLevel, cryptoContext.SecurityLevel);
        }

        [Fact]
        public void TestCreateWithSecurityLevelTooHigh()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CryptoContext.CreateWithSecurityLevel(280));
        }

        [Fact]
        public void TestCreateDefault()
        {
            var cryptoContext = CryptoContext.CreateDefault();
            Assert.Equal(128, cryptoContext.SecurityLevel);
        }

        [Fact]
        public void TestDispose()
        {
            var randomNumberGenerator = new TestRandomNumberGenerator();
            var hashAlgorithmProvider = new SHA256Provider();

            Assert.False(randomNumberGenerator.Disposed);

            using (var cryptoContext = new CryptoContext(randomNumberGenerator, hashAlgorithmProvider)) { }
            Assert.True(randomNumberGenerator.Disposed);
        }


    }
}