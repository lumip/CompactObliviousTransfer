using Moq;
using Xunit;
using System.Security.Cryptography;
using System.Diagnostics;

namespace CompactOT.Adapters
{

    public class RandomFromCorrelatedObliviousTransferChannelTests
    {

        private ICorrelatedObliviousTransferChannel GetBaseTransferChannel(IMessageChannel messageChannel)
        {
            var otMock = new Mock<InsecureObliviousTransfer>() { CallBase = true };
            otMock.Setup(ot => ot.SecurityLevel).Returns(10000000);
            // TODO: heavily now depends on other objects; possible to create better mocks to make this really a unit test?
            return new CorrelatedFromStandardObliviousTransferChannel(
                new StatelessObliviousTransferChannel(otMock.Object, messageChannel),
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
        public void TestProtocol()
        {
            var messageChannels = new TestMessageChannels();
            var otSender = new RandomFromCorrelatedObliviousTransferChannel(
                GetBaseTransferChannel(messageChannels.FirstPartyChannel), RandomNumberGenerator.Create()
            );
            var otReceiver = new RandomFromCorrelatedObliviousTransferChannel(
                GetBaseTransferChannel(messageChannels.SecondPartyChannel), RandomNumberGenerator.Create()
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

    }
}
