// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Diagnostics;
using System.Linq;
using CompactOT.Codes;
using Moq;
using Xunit;

namespace CompactOT
{
    public class CorrelatedObliviousTransferChannelTests
    {

        [Fact]
        public async void TestCorrleatedOTs()
        {
            int numberOfOptions = TestUtils.TestCorrelations.Length + 1;

            int securityLevel = 8;

            var messageChannels = new TestMessageChannels();
            var senderBaseChannel = TestUtils.GetBaseTransferChannel(messageChannels.FirstPartyChannel);
            var receiverBaseChannel = TestUtils.GetBaseTransferChannel(messageChannels.SecondPartyChannel);

            var cryptoContext = CryptoContext.CreateDefault();
            var code = WalshHadamardCode.CreateWithDistance(cryptoContext.SecurityLevel);
            var otSender = new CorrelatedObliviousTransferChannel(senderBaseChannel, securityLevel, cryptoContext, code);
            var otReceiver = new CorrelatedObliviousTransferChannel(receiverBaseChannel, securityLevel, cryptoContext, code);

            const int numberOfInvocations = 3;
            int numberOfMessageBits = TestUtils.TestCorrelations[0].Length;

            // sender data
            var correlations = new ObliviousTransferOptions(numberOfInvocations, numberOfOptions - 1, numberOfMessageBits);

            correlations.SetInvocation(0, TestUtils.TestCorrelations.ToArray());
            correlations.SetInvocation(1, TestUtils.TestCorrelations.ToArray());
            correlations.SetInvocation(2, TestUtils.TestCorrelations.ToArray());

            // receiver data
            var receiverIndices = new int[] { 0, 3, numberOfOptions - 1 };

            // execute protocol
            var sendTask = otSender.SendAsync(correlations);
            var receiverTask = otReceiver.ReceiveAsync(receiverIndices, numberOfOptions, numberOfMessageBits);

            await TestUtils.WhenAllOrFail(sendTask, receiverTask);

            // verify results
            ObliviousTransferResult senderResults = await sendTask;
            ObliviousTransferResult results = await receiverTask;
            Assert.Equal(numberOfInvocations, results.NumberOfInvocations);
            Assert.Equal(numberOfMessageBits, results.NumberOfMessageBits);
            Assert.Equal(numberOfInvocations, senderResults.NumberOfInvocations);
            Assert.Equal(numberOfMessageBits, senderResults.NumberOfMessageBits);

            Debug.Assert(receiverIndices[0] == 0);
            var expectedFirst = senderResults.GetInvocationResult(0);
            Assert.Equal(expectedFirst, results.GetInvocationResult(0));

            Debug.Assert(receiverIndices[1] != 0);
            var expectedSecond = correlations.GetMessage(1, receiverIndices[1] - 1) ^ senderResults.GetInvocationResult(1);
            Assert.Equal(expectedSecond, results.GetInvocationResult(1));

            Debug.Assert(receiverIndices[2] == numberOfOptions - 1);
            var expectedThird = correlations.GetMessage(2, receiverIndices[2] - 1) ^ senderResults.GetInvocationResult(2);
            Assert.Equal(expectedThird, results.GetInvocationResult(2));
        }

        [Fact]
        public void TestEstimateCostNoMaxNumberOfInvocations()
        {
            int securityLevel = 4;

            var baseOTMock = new Mock<IObliviousTransferChannel>();
            baseOTMock.Setup(ot => ot.SecurityLevel).Returns(1000000);

            var cryptoContext = CryptoContext.CreateDefault();
            var code = WalshHadamardCode.CreateWithDistance(securityLevel);

            var otProtocol = new CorrelatedObliviousTransferChannel(
                baseOTMock.Object, securityLevel, cryptoContext, code
            );

            var usageProjection = new ObliviousTransferUsageProjection();
            Assert.True(double.IsPositiveInfinity(otProtocol.EstimateCost(usageProjection)));
        }

        [Fact]
        public void TestEstimateCost()
        {
            int securityLevel = 4;

            var baseOTMock = new Mock<IObliviousTransferChannel>();
            baseOTMock.Setup(ot => ot.SecurityLevel).Returns(1000000);
            double baseCost = 3.5;
            baseOTMock.Setup(ot => ot.EstimateCost(It.IsAny<ObliviousTransferUsageProjection>())).Returns(baseCost);

            var cryptoContext = CryptoContext.CreateDefault();

            int codeLength = 15;
            var codeMock = new Mock<IBinaryCode>();
            codeMock.Setup(c => c.CodeLength).Returns(codeLength);
            codeMock.Setup(c => c.Distance).Returns(securityLevel);
            var code = codeMock.Object;

            var otProtocol = new CorrelatedObliviousTransferChannel(
                baseOTMock.Object, securityLevel, cryptoContext, code
            );

            var usageProjection = new ObliviousTransferUsageProjection
            {
                MaxNumberOfInvocations = 7,
                AverageNumberOfOptions = 3,
                AverageMessageBits = 11
            };

            double initialExchangeCost = usageProjection.MaxNumberOfBatches * usageProjection.AverageInvocationsPerBatch * codeLength;
            double maskedOptionExchangeCost = usageProjection.MaxNumberOfInvocations * (usageProjection.AverageNumberOfOptions - 1) * usageProjection.AverageMessageBits;

            double expectedCost = baseCost + initialExchangeCost + maskedOptionExchangeCost;

            double actualCost = otProtocol.EstimateCost(usageProjection);

            Assert.Equal(expectedCost, actualCost);
        }

    }
}
