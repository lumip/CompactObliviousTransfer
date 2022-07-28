using System;
using Xunit;
using Moq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Linq;

using CompactCryptoGroupAlgebra;
using CompactOT.DataStructures;

namespace CompactOT
{
    public class ALSZRandomObliviousTransferChannelTests
    {

        private IObliviousTransferChannel GetBaseTransferChannel(IMessageChannel messageChannel)
        {
            var otMock = new Mock<InsecureObliviousTransfer>() { CallBase = true };
            otMock.Setup(ot => ot.SecurityLevel).Returns(10000000);
            return new StatelessObliviousTransferChannel(otMock.Object, messageChannel);
        }

        [Fact]
        public void TestRandomOTs()
        {
            const int numberOfOptions = 6;
            const int numberOfInvocations = 2;
            const int numberOfMessageBits = 5;

            var securityParameter = NumberLength.FromBitLength(8);

            var messageChannels = new TestMessageChannels();
            var senderBaseChannel = GetBaseTransferChannel(messageChannels.FirstPartyChannel);
            var receiverBaseChannel = GetBaseTransferChannel(messageChannels.SecondPartyChannel);

            var cryptoContext = CryptoContext.CreateDefault();
            var otSender = new ALSZRandomObliviousTransferChannel(senderBaseChannel, securityParameter.InBits, cryptoContext);
            var otReceiver = new ALSZRandomObliviousTransferChannel(receiverBaseChannel, securityParameter.InBits, cryptoContext);

            // receiver data
            var receiverIndices = new int[] { 0, 3 };

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
