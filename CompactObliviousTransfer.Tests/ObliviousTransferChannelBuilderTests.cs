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
            Assert.False(otChannel is ALSZRandomObliviousTransferChannel);
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
            Assert.True(otChannel is ALSZRandomObliviousTransferChannel);
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
            Assert.True(otChannel is ALSZRandomObliviousTransferChannel);
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
            Assert.False(otChannel is ALSZCorrelatedObliviousTransferChannel);
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
            Assert.True(otChannel is ALSZCorrelatedObliviousTransferChannel);
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
            Assert.True(otChannel is ALSZCorrelatedObliviousTransferChannel);
        }
    }

}
