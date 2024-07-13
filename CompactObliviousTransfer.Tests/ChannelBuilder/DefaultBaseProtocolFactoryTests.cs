// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Numerics;
using System.Security.Cryptography;

using CompactCryptoGroupAlgebra.EllipticCurves;
using Moq;
using Xunit;

namespace CompactOT
{

    public class DefaultBaseProtocolFactoryTests
    {

        [Fact]
        public void TestMakeChannelSecurityLevelSmallEnoughForEC()
        {
            int securityLevel = 128;

            var messageChannelMock = new Mock<IMessageChannel>();
            var messageChannel = messageChannelMock.Object;
            var cryptoContext = CryptoContext.CreateDefault();
            var factory = new DefaultBaseProtocolFactory();
            var otChannel = factory.MakeChannel(messageChannel, cryptoContext, securityLevel);

            Assert.Same(messageChannel, otChannel.Channel);
            Assert.Equal(securityLevel, otChannel.SecurityLevel);
            Assert.True(otChannel is NaorPinkasObliviousTransferChannel<BigInteger, CurvePoint>);
        }

        [Fact]
        public void TestMakeChannelSecurityLevelTooLargeForEC()
        {
            int securityLevel = 384;

            var messageChannelMock = new Mock<IMessageChannel>();
            var messageChannel = messageChannelMock.Object;

            var hashAlgorithmProviderMock = new Mock<HashAlgorithmProvider>();
            hashAlgorithmProviderMock.Setup(hap => hap.SecurityLevel).Returns(512);
            var cryptoContext = new CryptoContext(RandomNumberGenerator.Create(), hashAlgorithmProviderMock.Object);

            var factory = new DefaultBaseProtocolFactory();
            Assert.Throws<ArgumentOutOfRangeException>(() => factory.MakeChannel(messageChannel, cryptoContext, securityLevel));
        }

        [Fact]
        public void TestMakeChannelCryptoContextSecurityLevelTooSmall()
        {
            int securityLevel = 256;

            var messageChannelMock = new Mock<IMessageChannel>();
            var messageChannel = messageChannelMock.Object;

            var hashAlgorithmProviderMock = new Mock<HashAlgorithmProvider>();
            hashAlgorithmProviderMock.Setup(hap => hap.SecurityLevel).Returns(128);
            var cryptoContext = new CryptoContext(RandomNumberGenerator.Create(), hashAlgorithmProviderMock.Object);

            var factory = new DefaultBaseProtocolFactory();

            Assert.Throws<ArgumentException>(() => factory.MakeChannel(messageChannel, cryptoContext, securityLevel));
        }

    }

}
