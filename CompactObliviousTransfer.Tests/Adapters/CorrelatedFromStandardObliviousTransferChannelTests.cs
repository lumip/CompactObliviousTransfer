using Moq;
using Xunit;
using System.Security.Cryptography;
using System.Diagnostics;

namespace CompactOT.Adapters
{

    public class CorrelatedFromStandardObliviousTransferChannelTests
    {

        private IObliviousTransferChannel GetBaseTransferChannel(IMessageChannel messageChannel)
        {
            var otMock = new Mock<InsecureObliviousTransfer>() { CallBase = true };
            otMock.Setup(ot => ot.SecurityLevel).Returns(10000000);
            return new StatelessObliviousTransferChannel(otMock.Object, messageChannel);
        }

        [Fact]
        public void TestSecurityLevel()
        {
            
            var messageChannels = new TestMessageChannels();
            var baseOt = GetBaseTransferChannel(messageChannels.FirstPartyChannel);
            var cot = new CorrelatedFromStandardObliviousTransferChannel(
                baseOt, RandomNumberGenerator.Create()
            );
            Assert.Equal(baseOt.SecurityLevel, cot.SecurityLevel);
        }

        [Fact]
        public void TestChannel()
        {
            var messageChannels = new TestMessageChannels();
            var baseOt = GetBaseTransferChannel(messageChannels.FirstPartyChannel);
            var cot = new CorrelatedFromStandardObliviousTransferChannel(
                baseOt, RandomNumberGenerator.Create()
            );
            Assert.Same(baseOt.Channel, cot.Channel);
        }

        [Fact]
        public void TestProtocol()
        {
            var messageChannels = new TestMessageChannels();
            var otSender = new CorrelatedFromStandardObliviousTransferChannel(
                GetBaseTransferChannel(messageChannels.FirstPartyChannel), RandomNumberGenerator.Create()
            );
            var otReceiver = new CorrelatedFromStandardObliviousTransferChannel(
                GetBaseTransferChannel(messageChannels.SecondPartyChannel), RandomNumberGenerator.Create()
            );

            int numberOfInvocations = 2;
            int numberOfCorrelations = TestUtils.TestCorrelations.Length;
            int numberOfOptions = numberOfCorrelations + 1;
            int numberOfMessageBits = TestUtils.TestCorrelations[0].Length;

            var correlations = new ObliviousTransferOptions(numberOfInvocations, numberOfCorrelations, numberOfMessageBits);
            correlations.SetInvocation(0, TestUtils.TestCorrelations);
            correlations.SetInvocation(1, TestUtils.TestCorrelations);

            int[] receiverIndices = new int[] { 0, 4 };

            var senderTask = otSender.SendAsync(correlations);
            var receiverTask = otReceiver.ReceiveAsync(receiverIndices, numberOfOptions, numberOfMessageBits);

            TestUtils.WaitAllOrFail(senderTask, receiverTask);

            var senderResults = senderTask.Result;
            var receiverResults = receiverTask.Result;

            Assert.Equal(numberOfInvocations, senderResults.NumberOfInvocations);
            Assert.Equal(numberOfMessageBits, senderResults.NumberOfMessageBits);
            Assert.Equal(numberOfInvocations, receiverResults.NumberOfInvocations);
            Assert.Equal(numberOfMessageBits, receiverResults.NumberOfMessageBits);

            Debug.Assert(receiverIndices[0] == 0);
            var expectedFirst = senderResults.GetInvocationResult(0);
            Assert.Equal(expectedFirst, receiverResults.GetInvocationResult(0));

            Debug.Assert(receiverIndices[1] != 0);
            var expectedSecond = correlations.GetMessage(1, receiverIndices[1] - 1) ^ senderResults.GetInvocationResult(1);
            Assert.Equal(expectedSecond, receiverResults.GetInvocationResult(1));
        }
        
    }
}
