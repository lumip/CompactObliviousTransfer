// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Linq;
using System.Text;
using System.Threading;
using CompactOT.Codes;
using Moq;
using Xunit;

namespace CompactOT
{
    public class ExtendedObliviousTransferChannelTests
    {

        [Fact]
        public async void TestBaseOTs()
        {
            int securityLevel = 24;

            var messageChannels = new TestMessageChannels();
            var senderBaseChannel = TestUtils.GetBaseTransferChannel(messageChannels.FirstPartyChannel);
            var receiverBaseChannel = TestUtils.GetBaseTransferChannel(messageChannels.SecondPartyChannel);

            var cryptoContext = CryptoContext.CreateDefault();
            var code = WalshHadamardCode.CreateWithDistance(cryptoContext.SecurityLevel);

            var otSender = new ExtendedObliviousTransferChannel(senderBaseChannel, securityLevel, cryptoContext, code);
            var otReceiver = new ExtendedObliviousTransferChannel(receiverBaseChannel, securityLevel, cryptoContext, code);

            var tokenSource = new CancellationTokenSource(TestUtils.TestTimeoutMs);

            var senderTask = otSender.ExecuteSenderBaseTransferAsync(tokenSource.Token);
            var receiverTask = otReceiver.ExecuteReceiverBaseTransferAsync(tokenSource.Token);

            await TestUtils.WhenAllOrFail(senderTask, receiverTask);
        }

        [Fact]
        public async void TestExtendedOTs()
        {
            int numberOfOptions = TestUtils.TestOptions.Length;

            int securityLevel = 8;

            var messageChannels = new TestMessageChannels();
            var senderBaseChannel = TestUtils.GetBaseTransferChannel(messageChannels.FirstPartyChannel);
            var receiverBaseChannel = TestUtils.GetBaseTransferChannel(messageChannels.SecondPartyChannel);

            var cryptoContext = CryptoContext.CreateDefault();
            var code = WalshHadamardCode.CreateWithDistance(cryptoContext.SecurityLevel);

            IObliviousTransferChannel otSender = new ExtendedObliviousTransferChannel(senderBaseChannel, securityLevel, cryptoContext, code);
            IObliviousTransferChannelReceiverEndpoint otReceiver = new ExtendedObliviousTransferChannel(receiverBaseChannel, securityLevel, cryptoContext, code);

            const int numberOfInvocations = 3;
            int numberOfMessageBits = TestUtils.TestOptions[0].Length * 8;

            // sender data
            var options = new ObliviousTransferOptions(numberOfInvocations, numberOfOptions, numberOfMessageBits);

            options.SetInvocation(0, TestUtils.TestOptions.Select(s => Encoding.ASCII.GetBytes(s)).ToArray());
            options.SetInvocation(1, TestUtils.TestOptions.Select(s => Encoding.ASCII.GetBytes(s.ToLower())).ToArray());
            options.SetInvocation(2, TestUtils.TestOptions.Select(s => Encoding.ASCII.GetBytes(s.ToUpper())).ToArray());

            // receiver data
            var receiverIndices = new int[] { 0, 5, 3 };

            // execute protocol
            var tokenSource = new CancellationTokenSource(TestUtils.TestTimeoutMs);

            var sendTask = otSender.SendAsync(options, tokenSource.Token);
            var receiverTask = otReceiver.ReceiveAsync(receiverIndices, numberOfOptions, numberOfMessageBits, tokenSource.Token);

            await TestUtils.WhenAllOrFail(sendTask, receiverTask);

            // verify results
            ObliviousTransferResult results = await receiverTask;
            Assert.Equal(numberOfInvocations, results.NumberOfInvocations);
            Assert.Equal(numberOfMessageBits, results.NumberOfMessageBits);
            for (int i = 0; i < results.NumberOfInvocations; ++i)
            {
                var expected = options.GetMessage(i, receiverIndices[i]);
                Assert.Equal(expected, results.GetInvocationResult(i));
            }
        }

        [Fact]
        public async void TestSendAsyncRejectsBadNumberOfOptions()
        {
            int numberOfOptions = TestUtils.TestOptions.Length;

            int securityLevel = 8;

            var cryptoContext = CryptoContext.CreateDefault();

            var codeMock = new Mock<IBinaryCode>();
            codeMock.Setup(c => c.CodeLength).Returns(numberOfOptions - 1);
            codeMock.Setup(c => c.Distance).Returns(securityLevel);
            var code = codeMock.Object;

            var baseOtChannelMock = new Mock<IObliviousTransferChannel>();
            baseOtChannelMock.Setup(bot => bot.SecurityLevel).Returns(securityLevel);
            var baseOtChannel = baseOtChannelMock.Object;

            IObliviousTransferChannel otChannel = new ExtendedObliviousTransferChannel(baseOtChannel, securityLevel, cryptoContext, code);

            int numberOfMessageBits = TestUtils.TestOptions[0].Length * 8;

            // receiver data
            var receiverIndices = new int[] { 0, 5, 3 };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await otChannel.ReceiveAsync(receiverIndices, numberOfOptions, numberOfMessageBits)
            );
        }

        [Fact]
        public async void TestReceiverComputeAndSendURejectsBadNumberOfOptions()
        {
            int numberOfOptions = TestUtils.TestOptions.Length;

            int securityLevel = 8;

            var cryptoContext = CryptoContext.CreateDefault();

            var codeMock = new Mock<IBinaryCode>();
            codeMock.Setup(c => c.CodeLength).Returns(numberOfOptions - 1);
            codeMock.Setup(c => c.Distance).Returns(securityLevel);
            var code = codeMock.Object;

            var baseOtChannelMock = new Mock<IObliviousTransferChannel>();
            baseOtChannelMock.Setup(bot => bot.SecurityLevel).Returns(securityLevel);
            var baseOtChannel = baseOtChannelMock.Object;

            IObliviousTransferChannel otChannel = new ExtendedObliviousTransferChannel(baseOtChannel, securityLevel, cryptoContext, code);

            const int numberOfInvocations = 3;
            int numberOfMessageBits = TestUtils.TestOptions[0].Length * 8;

            // sender data
            var options = new ObliviousTransferOptions(numberOfInvocations, numberOfOptions, numberOfMessageBits);

            options.SetInvocation(0, TestUtils.TestOptions.Select(s => Encoding.ASCII.GetBytes(s)).ToArray());
            options.SetInvocation(1, TestUtils.TestOptions.Select(s => Encoding.ASCII.GetBytes(s.ToLower())).ToArray());
            options.SetInvocation(2, TestUtils.TestOptions.Select(s => Encoding.ASCII.GetBytes(s.ToUpper())).ToArray());

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await otChannel.SendAsync(options)
            );
        }

        [Fact]
        public void TestEstimateCostNoMaxNumberOfInvocations()
        {
            int securityLevel = 4;

            var baseOTMock = new Mock<IObliviousTransferChannel>();
            baseOTMock.Setup(ot => ot.SecurityLevel).Returns(1000000);

            var cryptoContext = CryptoContext.CreateDefault();
            var code = WalshHadamardCode.CreateWithDistance(securityLevel);

            var otProtocol = new ExtendedObliviousTransferChannel(
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

            var otProtocol = new ExtendedObliviousTransferChannel(
                baseOTMock.Object, securityLevel, cryptoContext, code
            );

            var usageProjection = new ObliviousTransferUsageProjection
            {
                MaxNumberOfInvocations = 7,
                AverageNumberOfOptions = 3,
                AverageMessageBits = 11
            };

            double initialExchangeCost = usageProjection.MaxNumberOfBatches * usageProjection.AverageInvocationsPerBatch * codeLength;
            double onlineCost = usageProjection.MaxNumberOfInvocations * usageProjection.AverageNumberOfOptions * usageProjection.AverageMessageBits;

            double expectedCost = baseCost + initialExchangeCost + onlineCost;

            double actualCost = otProtocol.EstimateCost(usageProjection);

            Assert.Equal(expectedCost, actualCost);
        }

    }
}
