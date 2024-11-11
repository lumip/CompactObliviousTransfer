// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using Moq;
using Xunit;

namespace CompactOT.Adapters
{

    public class RandomFromCorrelatedObliviousTransferChannelTests
    {

        private ICorrelatedObliviousTransferChannel GetBaseTransferChannel(IMessageChannel messageChannel)
        {
            // TODO: heavily now depends on other objects; possible to create better mocks to make this really a unit test?
            return new CorrelatedFromStandardObliviousTransferChannel(
                TestUtils.GetBaseTransferChannel(messageChannel),
                RandomNumberGenerator.Create()
            );
        }

        [Fact]
        public void TestSecurityLevel()
        {
            var messageChannels = new TestMessageChannels();
            var baseOt = GetBaseTransferChannel(messageChannels.FirstPartyChannel);
            var cot = new RandomFromCorrelatedObliviousTransferChannel(
                baseOt, RandomNumberGenerator.Create()
            );
            Assert.Equal(baseOt.SecurityLevel, cot.SecurityLevel);
        }

        [Fact]
        public void TestChannel()
        {
            var messageChannels = new TestMessageChannels();
            var baseOt = GetBaseTransferChannel(messageChannels.FirstPartyChannel);
            var cot = new RandomFromCorrelatedObliviousTransferChannel(
                baseOt, RandomNumberGenerator.Create()
            );
            Assert.Same(baseOt.Channel, cot.Channel);
        }

        [Fact]
        public async void TestProtocol()
        {
            var messageChannels = new TestMessageChannels();
            IRandomObliviousTransferChannel otSender = new RandomFromCorrelatedObliviousTransferChannel(
                GetBaseTransferChannel(messageChannels.FirstPartyChannel), RandomNumberGenerator.Create()
            );
            IObliviousTransferChannelReceiverEndpoint otReceiver = new RandomFromCorrelatedObliviousTransferChannel(
                GetBaseTransferChannel(messageChannels.SecondPartyChannel), RandomNumberGenerator.Create()
            );

            int numberOfInvocations = 2;
            int numberOfOptions = 7;
            int numberOfMessageBits = 11;

            int[] receiverIndices = new int[] { 0, 4 };

            var tokenSource = new CancellationTokenSource(TestUtils.TestTimeoutMs);

            var senderTask = otSender.SendAsync(numberOfInvocations, numberOfOptions, numberOfMessageBits, tokenSource.Token);
            var receiverTask = otReceiver.ReceiveAsync(receiverIndices, numberOfOptions, numberOfMessageBits, tokenSource.Token);

            await TestUtils.WhenAllOrFail(senderTask, receiverTask);

            var senderResults = await senderTask;
            var receiverResults = await receiverTask;

            Assert.Equal(numberOfInvocations, senderResults.NumberOfInvocations);
            Assert.Equal(numberOfOptions, senderResults.NumberOfOptions);
            Assert.Equal(numberOfMessageBits, senderResults.NumberOfMessageBits);
            Assert.Equal(numberOfInvocations, receiverResults.NumberOfInvocations);
            Assert.Equal(numberOfMessageBits, receiverResults.NumberOfMessageBits);

            Debug.Assert(receiverIndices[0] == 0);
            var expectedFirst = senderResults.GetMessage(0, receiverIndices[0]);
            Assert.Equal(expectedFirst, receiverResults.GetInvocationResult(0));

            Debug.Assert(receiverIndices[1] != 0);
            var expectedSecond = senderResults.GetMessage(1, receiverIndices[1]);
            Assert.Equal(expectedSecond, receiverResults.GetInvocationResult(1));
        }

        [Fact]
        public void TestEstimateCost()
        {
            var cOtChannelMock = new Mock<ICorrelatedObliviousTransferChannel>();
            var cOtChannel = cOtChannelMock.Object;

            double expectedCost = 75.0;
            cOtChannelMock.Setup(ot => ot.EstimateCost(It.IsAny<ObliviousTransferUsageProjection>())).Returns(expectedCost);

            var rOtChannel = new RandomFromCorrelatedObliviousTransferChannel(cOtChannel, RandomNumberGenerator.Create());

            var usageProjection = new ObliviousTransferUsageProjection();
            var cost = rOtChannel.EstimateCost(usageProjection);

            Assert.Equal(expectedCost, cost);
            cOtChannelMock.Verify(ot => ot.EstimateCost(It.IsAny<ObliviousTransferUsageProjection>()), Times.Once());
        }

    }
}
