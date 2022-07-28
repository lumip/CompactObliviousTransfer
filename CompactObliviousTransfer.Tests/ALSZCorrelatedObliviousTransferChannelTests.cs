using System;
using Xunit;
using Moq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Linq;
using System.Diagnostics;

using CompactCryptoGroupAlgebra;
using CompactOT.DataStructures;

namespace CompactOT
{
    public class ALSZCorrelatedObliviousTransferChannelTests
    {

        private IObliviousTransferChannel GetBaseTransferChannel(IMessageChannel messageChannel)
        {
            var otMock = new Mock<InsecureObliviousTransfer>() { CallBase = true };
            otMock.Setup(ot => ot.SecurityLevel).Returns(10000000);
            return new StatelessObliviousTransferChannel(otMock.Object, messageChannel);
        }

        private static readonly BitArray[] TestCorrelations = { 
            BitArray.FromBinaryString("000111"),
            BitArray.FromBinaryString("111000"),
            BitArray.FromBinaryString("100001"),
            BitArray.FromBinaryString("010010"),
            BitArray.FromBinaryString("101101"),
        };

        [Fact]
        public void TestCorrleatedOTs()
        {
            int numberOfOptions = TestCorrelations.Length + 1;

            var securityParameter = NumberLength.FromBitLength(8);

            var messageChannels = new TestMessageChannels();
            var senderBaseChannel = GetBaseTransferChannel(messageChannels.FirstPartyChannel);
            var receiverBaseChannel = GetBaseTransferChannel(messageChannels.SecondPartyChannel);

            var cryptoContext = CryptoContext.CreateDefault();
            var otSender = new ALSZCorrelatedObliviousTransferChannel(senderBaseChannel, securityParameter.InBits, cryptoContext);
            var otReceiver = new ALSZCorrelatedObliviousTransferChannel(receiverBaseChannel, securityParameter.InBits, cryptoContext);

            const int numberOfInvocations = 2;
            int numberOfMessageBits = TestCorrelations[0].Length;

            // sender data
            var correlations = new ObliviousTransferOptions(numberOfInvocations, numberOfOptions - 1, numberOfMessageBits);

            correlations.SetInvocation(0, TestCorrelations.ToArray());
            correlations.SetInvocation(1, TestCorrelations.ToArray());

            // receiver data
            var receiverIndices = new int[] { 0, 3 };

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
        }

    }
}
