// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using Xunit;
using Moq;

namespace CompactOT
{

    public class ObliviousTransferChannelBuilderTests
    {

        [Fact]
        public void TestMakeObliviousTransferChannelFewInvocations()
        {
            var builder = new ObliviousTransferChannelBuilder();

            var channelStub = new Mock<IMessageChannel>();  

            int securityLevel = 256;
            var otChannel = builder
                .WithSecurityLevel(securityLevel)
                .WithMaximumNumberOfOptions(3)
                .WithMaximumNumberOfInvocations(1)
                .MakeObliviousTransferChannel(channelStub.Object);

            Assert.True(otChannel.SecurityLevel >= securityLevel);
            Assert.False(otChannel is ExtendedObliviousTransferChannel);
        }

        [Fact]
        public void TestMakeObliviousTransferChannelManyInvocations()
        {
            var builder = new ObliviousTransferChannelBuilder();

            var channelStub = new Mock<IMessageChannel>();  

            int securityLevel = 256;
            var otChannel = builder
                .WithSecurityLevel(securityLevel)
                .WithMaximumNumberOfOptions(3)
                .WithMaximumNumberOfInvocations(50)
                .MakeObliviousTransferChannel(channelStub.Object);

            Assert.True(otChannel.SecurityLevel >= securityLevel);
            Assert.True(otChannel is ExtendedObliviousTransferChannel);
        }

        [Fact]
        public void TestMakeObliviousTransferChannelManyInvocationsInSingleBatch()
        {
            var builder = new ObliviousTransferChannelBuilder();

            var channelStub = new Mock<IMessageChannel>();  

            int securityLevel = 256;
            var otChannel = builder
                .WithSecurityLevel(securityLevel)
                .WithMaximumNumberOfOptions(3)
                .WithMaximumNumberOfBatches(1)
                .WithMaximumNumberOfInvocations(100)
                .MakeObliviousTransferChannel(channelStub.Object);

            Assert.True(otChannel.SecurityLevel >= securityLevel);
            Assert.False(otChannel is ExtendedObliviousTransferChannel);
        }

        [Fact]
        public void TestMakeObliviousTransferChannelTwoOptions()
        {
            var builder = new ObliviousTransferChannelBuilder();

            var channelStub = new Mock<IMessageChannel>();  

            int securityLevel = 256;
            var otChannel = builder
                .WithSecurityLevel(securityLevel)
                .WithMaximumNumberOfOptions(2)
                .WithMaximumNumberOfInvocations(50)
                .WithMaximumNumberOfBatches(4)
                .MakeObliviousTransferChannel(channelStub.Object);

            Assert.True(otChannel.SecurityLevel >= securityLevel);
            Assert.False(otChannel is ExtendedObliviousTransferChannel);
        }

        [Fact]
        public void TestMakeObliviousTransferChannelUnlimitedInvocations()
        {
            var builder = new ObliviousTransferChannelBuilder();

            var channelStub = new Mock<IMessageChannel>();  

            int securityLevel = 128;
            var otChannel = builder
                .WithSecurityLevel(securityLevel)
                .WithMaximumNumberOfOptions(3)
                .MakeObliviousTransferChannel(channelStub.Object);

            Assert.True(otChannel.SecurityLevel >= securityLevel);
            Assert.True(otChannel is ExtendedObliviousTransferChannel);
        }

        [Fact]
        public void TestMakeObliviousTransferChannelCustomCryptoContext()
        {
            var builder = new ObliviousTransferChannelBuilder();

            var channelStub = new Mock<IMessageChannel>();

            var cryptoContext = CryptoContext.CreateWithSecurityLevel(256);

            int securityLevel = 128;
            var otChannel = builder
                .WithSecurityLevel(securityLevel)
                .WithMaximumNumberOfOptions(3)
                .MakeObliviousTransferChannel(channelStub.Object, cryptoContext);

            Assert.True(otChannel.SecurityLevel >= securityLevel);
            Assert.True(otChannel is ExtendedObliviousTransferChannel);
        }

        [Fact]
        public void TestMakeObliviousTransferChannelCustomCryptoContextInsufficient()
        {
            var builder = new ObliviousTransferChannelBuilder();

            var channelStub = new Mock<IMessageChannel>();

            var cryptoContext = CryptoContext.CreateWithSecurityLevel(128);

            int securityLevel = 256;
            var otChannel = builder
                .WithSecurityLevel(securityLevel)
                .WithMaximumNumberOfOptions(3);

            Assert.Throws<ArgumentException>(
                () => otChannel.MakeObliviousTransferChannel(channelStub.Object, cryptoContext)
            );
        }

        [Fact]
        public void TestMakeObliviousTransferChannelWithCustomBaseOT()
        {
            var builder = new ObliviousTransferChannelBuilder();

            var channelStub = new Mock<IMessageChannel>();
            int securityLevel = 128;

            var baseProtocolMock = new Mock<IObliviousTransferChannel>();
            baseProtocolMock
                .Setup(p => p.EstimateCost(It.IsAny<ObliviousTransferUsageProjection>()))
                .Returns(100);
            baseProtocolMock
                .Setup(p => p.SecurityLevel)
                .Returns(securityLevel);
            var baseProtocol = baseProtocolMock.Object;

            var baseProtocolFactoryMock = new Mock<IBaseProtocolFactory>();
            baseProtocolFactoryMock
                .Setup(f => f.MakeChannel(
                    It.IsAny<IMessageChannel>(), It.IsAny<CryptoContext>(), It.IsAny<int>()
                ))
                .Returns(baseProtocol);

            var baseProtocolFactory = baseProtocolFactoryMock.Object;

            var otChannel = builder
                .WithSecurityLevel(securityLevel)
                .WithMaximumNumberOfInvocations(1)
                .WithCustomBaseProtocol(baseProtocolFactory)
                .MakeObliviousTransferChannel(channelStub.Object);

            Assert.True(otChannel.SecurityLevel >= securityLevel);
            Assert.False(otChannel is ExtendedObliviousTransferChannel);
        }

        [Fact]
        public void TestMakeOblivousTransferChannelWithAvgNumOptions()
        {
            var builder = new ObliviousTransferChannelBuilder();

            var channelStub = new Mock<IMessageChannel>();

            int securityLevel = 64;
            var otChannel = builder
                .WithSecurityLevel(securityLevel)
                .WithAverageNumberOfOptions(5)
                .WithMaximumNumberOfInvocations(10)
                .WithAverageMessageBits(1000)
                .MakeObliviousTransferChannel(channelStub.Object);

            Assert.True(otChannel.SecurityLevel >= securityLevel);
            Assert.True(otChannel is ExtendedObliviousTransferChannel);
        }

        [Fact]
        public void TestMakeObliviousTransferChannelWithAvgNumInvocations()
        {
            var builder = new ObliviousTransferChannelBuilder();

            var channelStub = new Mock<IMessageChannel>();

            int securityLevel = 64;
            var otChannel = builder
                .WithSecurityLevel(securityLevel)
                .WithMaximumNumberOfOptions(3)
                .WithAverageInvocationsPerBatch(10)
                .WithAverageMessageBits(1000)
                .WithMaximumNumberOfBatches(23)
                .MakeObliviousTransferChannel(channelStub.Object);

            Assert.True(otChannel.SecurityLevel >= securityLevel);
            Assert.True(otChannel is ExtendedObliviousTransferChannel);
        }

        [Fact]
        public void TestMakeRandomObliviousTransferChannelFewInvocations()
        {
            var builder = new ObliviousTransferChannelBuilder();

            var channelStub = new Mock<IMessageChannel>();  

            int securityLevel = 256;
            var otChannel = builder
                .WithSecurityLevel(securityLevel)
                .WithMaximumNumberOfOptions(3)
                .WithMaximumNumberOfInvocations(1)
                .MakeRandomObliviousTransferChannel(channelStub.Object);

            Assert.True(otChannel.SecurityLevel >= securityLevel);
            Assert.False(otChannel is RandomObliviousTransferChannel);
        }

        [Fact]
        public void TestMakeRandomObliviousTransferChannelManyInvocations()
        {
            var builder = new ObliviousTransferChannelBuilder();

            var channelStub = new Mock<IMessageChannel>();  

            int securityLevel = 256;
            var otChannel = builder
                .WithSecurityLevel(securityLevel)
                .WithMaximumNumberOfOptions(3)
                .WithMaximumNumberOfInvocations(50)
                .MakeRandomObliviousTransferChannel(channelStub.Object);

            Assert.True(otChannel.SecurityLevel >= securityLevel);
            Assert.True(otChannel is RandomObliviousTransferChannel);
        }

        [Fact]
        public void TestMakeRandomObliviousTransferChannelUnlimitedInvocations()
        {
            var builder = new ObliviousTransferChannelBuilder();

            var channelStub = new Mock<IMessageChannel>();  

            int securityLevel = 128;
            var otChannel = builder
                .WithSecurityLevel(securityLevel)
                .WithMaximumNumberOfOptions(3)
                .MakeRandomObliviousTransferChannel(channelStub.Object);

            Assert.True(otChannel.SecurityLevel >= securityLevel);
            Assert.True(otChannel is RandomObliviousTransferChannel);
        }

        [Fact]
        public void TestMakeCorrelatedObliviousTransferChannelFewInvocations()
        {
            var builder = new ObliviousTransferChannelBuilder();

            var channelStub = new Mock<IMessageChannel>();  

            int securityLevel = 256;
            var otChannel = builder
                .WithSecurityLevel(securityLevel)
                .WithMaximumNumberOfOptions(3)
                .WithMaximumNumberOfInvocations(1)
                .MakeCorrelatedObliviousTransferChannel(channelStub.Object);

            Assert.True(otChannel.SecurityLevel >= securityLevel);
            Assert.False(otChannel is CorrelatedObliviousTransferChannel);
        }

        [Fact]
        public void TestMakeCorrelatedObliviousTransferChannelManyInvocations()
        {
            var builder = new ObliviousTransferChannelBuilder();

            var channelStub = new Mock<IMessageChannel>();  

            int securityLevel = 256;
            var otChannel = builder
                .WithSecurityLevel(securityLevel)
                .WithMaximumNumberOfOptions(3)
                .WithMaximumNumberOfInvocations(50)
                .MakeCorrelatedObliviousTransferChannel(channelStub.Object);

            Assert.True(otChannel.SecurityLevel >= securityLevel);
            Assert.True(otChannel is CorrelatedObliviousTransferChannel);
        }

        [Fact]
        public void TestMakeCorrelatedObliviousTransferChannelUnlimitedInvocations()
        {
            var builder = new ObliviousTransferChannelBuilder();

            var channelStub = new Mock<IMessageChannel>();  

            int securityLevel = 128;
            var otChannel = builder
                .WithSecurityLevel(securityLevel)
                .WithMaximumNumberOfOptions(3)
                .MakeCorrelatedObliviousTransferChannel(channelStub.Object);

            Assert.True(otChannel.SecurityLevel >= securityLevel);
            Assert.True(otChannel is CorrelatedObliviousTransferChannel);
        }
    }

}
