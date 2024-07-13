// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Diagnostics;
using System.Security.Cryptography;

using Moq;
using Xunit;

namespace CompactOT.Adapters
{

    public class RandomFromStandardObliviousTransferChannelTests
    {

        [Fact]
        public void TestSecurityLevel()
        {
            var messageChannels = new TestMessageChannels();
            var baseOt = TestUtils.GetBaseTransferChannel(messageChannels.FirstPartyChannel);
            var cot = new RandomFromStandardObliviousTransferChannel(
                baseOt, RandomNumberGenerator.Create()
            );
            Assert.Equal(baseOt.SecurityLevel, cot.SecurityLevel);
        }

        [Fact]
        public void TestChannel()
        {
            var messageChannels = new TestMessageChannels();
            var baseOt = TestUtils.GetBaseTransferChannel(messageChannels.FirstPartyChannel);
            var cot = new RandomFromStandardObliviousTransferChannel(
                baseOt, RandomNumberGenerator.Create()
            );
            Assert.Same(baseOt.Channel, cot.Channel);
        }

        [Fact]
        public void TestProtocol()
        {
            var messageChannels = new TestMessageChannels();
            var otSender = new RandomFromStandardObliviousTransferChannel(
                TestUtils.GetBaseTransferChannel(messageChannels.FirstPartyChannel), RandomNumberGenerator.Create()
            );
            var otReceiver = new RandomFromStandardObliviousTransferChannel(
                TestUtils.GetBaseTransferChannel(messageChannels.SecondPartyChannel), RandomNumberGenerator.Create()
            );

            int numberOfInvocations = 2;
            int numberOfOptions = 7;
            int numberOfMessageBits = 11;

            int[] receiverIndices = new int[] { 0, 4 };

            var senderTask = otSender.SendAsync(numberOfInvocations, numberOfOptions, numberOfMessageBits);
            var receiverTask = otReceiver.ReceiveAsync(receiverIndices, numberOfOptions, numberOfMessageBits);

            TestUtils.WaitAllOrFail(senderTask, receiverTask);

            var senderResults = senderTask.Result;
            var receiverResults = receiverTask.Result;

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
            var baseOtChannelMock = new Mock<IObliviousTransferChannel>();
            var baseOtChannel = baseOtChannelMock.Object;

            double expectedCost = 75.0;
            baseOtChannelMock.Setup(ot => ot.EstimateCost(It.IsAny<ObliviousTransferUsageProjection>())).Returns(expectedCost);

            var rOtChannel = new RandomFromStandardObliviousTransferChannel(baseOtChannel, RandomNumberGenerator.Create());

            var usageProjection = new ObliviousTransferUsageProjection();
            var cost = rOtChannel.EstimateCost(usageProjection);

            Assert.Equal(expectedCost, cost);
            baseOtChannelMock.Verify(ot => ot.EstimateCost(It.IsAny<ObliviousTransferUsageProjection>()), Times.Once());
        }

    }
}
