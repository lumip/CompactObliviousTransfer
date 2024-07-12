// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using Xunit;
using Moq;

using CompactOT.Codes;

namespace CompactOT
{
    public class RandomObliviousTransferChannelTests
    {

        [Fact]
        public void TestRandomOTs()
        {
            const int numberOfOptions = 6;
            const int numberOfInvocations = 3;
            const int numberOfMessageBits = 5;

            int securityLevel = 8;

            var messageChannels = new TestMessageChannels();
            var senderBaseChannel = TestUtils.GetBaseTransferChannel(messageChannels.FirstPartyChannel);
            var receiverBaseChannel = TestUtils.GetBaseTransferChannel(messageChannels.SecondPartyChannel);

            var cryptoContext = CryptoContext.CreateDefault();
            var code = WalshHadamardCode.CreateWithDistance(cryptoContext.SecurityLevel);

            var otSender = new RandomObliviousTransferChannel(senderBaseChannel, securityLevel, cryptoContext, code);
            var otReceiver = new RandomObliviousTransferChannel(receiverBaseChannel, securityLevel, cryptoContext, code);

            // receiver data
            var receiverIndices = new int[] { 0, 3, numberOfOptions - 1 };

            // execute protocol
            var sendTask = otSender.SendAsync(numberOfInvocations, numberOfOptions, numberOfMessageBits);
            var receiverTask = otReceiver.ReceiveAsync(receiverIndices, numberOfOptions, numberOfMessageBits);

            TestUtils.WaitAllOrFail(sendTask, receiverTask);

            // verify results
            ObliviousTransferOptions senderResults = sendTask.Result;
            ObliviousTransferResult results = receiverTask.Result;
            Assert.Equal(numberOfInvocations, results.NumberOfInvocations);
            Assert.Equal(numberOfMessageBits, results.NumberOfMessageBits);
            Assert.Equal(numberOfInvocations, senderResults.NumberOfInvocations);
            Assert.Equal(numberOfOptions, senderResults.NumberOfOptions);
            Assert.Equal(numberOfMessageBits, senderResults.NumberOfMessageBits);

            for (int i = 0; i < numberOfInvocations; ++i)
            {
                var senderOption = senderResults.GetMessage(i, receiverIndices[i]);
                var receiverOption = results.GetInvocationResult(i);
                Assert.Equal(senderOption, receiverOption);
            }
        }

    }
}
