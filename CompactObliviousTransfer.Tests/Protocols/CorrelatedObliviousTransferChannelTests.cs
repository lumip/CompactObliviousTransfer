// SPDX-FileCopyrightText: 2023 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using Xunit;
using Moq;
using System.Linq;
using System.Diagnostics;

using CompactCryptoGroupAlgebra;
using CompactOT.Codes;

namespace CompactOT
{
    public class CorrelatedObliviousTransferChannelTests
    {

        [Fact]
        public void TestCorrleatedOTs()
        {
            int numberOfOptions = TestUtils.TestCorrelations.Length + 1;

            var securityParameter = NumberLength.FromBitLength(8);

            var messageChannels = new TestMessageChannels();
            var senderBaseChannel = TestUtils.GetBaseTransferChannel(messageChannels.FirstPartyChannel);
            var receiverBaseChannel = TestUtils.GetBaseTransferChannel(messageChannels.SecondPartyChannel);

            var cryptoContext = CryptoContext.CreateDefault();
            var code = WalshHadamardCode.CreateWithDistance(cryptoContext.SecurityLevel);
            var otSender = new CorrelatedObliviousTransferChannel(senderBaseChannel, securityParameter.InBits, cryptoContext, code);
            var otReceiver = new CorrelatedObliviousTransferChannel(receiverBaseChannel, securityParameter.InBits, cryptoContext, code);

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

            TestUtils.WaitAllOrFail(sendTask, receiverTask);

            // verify results
            ObliviousTransferResult senderResults = sendTask.Result;
            ObliviousTransferResult results = receiverTask.Result;
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

    }
}
