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

        private ObliviousTransferChannel GetBaseTransferChannel(IMessageChannel messageChannel)
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

        [Fact]
        public void TestSenderBaseOTs()
        {
            var securityParameter = NumberLength.FromBitLength(24);
            int codeLength = 2 * securityParameter.InBits;

            var received = new ObliviousTransferResult(codeLength, securityParameter.InBits);
            for (int j = 0; j < codeLength; ++j)
            {
                byte[] receivedAsBytes = new byte[securityParameter.InBytes];
                for (int i = 0; i < securityParameter.InBytes; ++i)
                {
                    receivedAsBytes[i] = (byte)(j*10 + i);
                }
                received.SetRow(j, new EnumeratedBitArrayView(receivedAsBytes, securityParameter.InBits));
            }

            var baseOTMock = new Mock<ObliviousTransferChannel>();
            baseOTMock.Setup(ot => ot.ReceiveAsync(It.IsAny<BitArray>(), It.IsAny<int>()))
                .Returns(Task.FromResult(received));
            baseOTMock.Setup(ot => ot.SecurityLevel).Returns(1000000);

            var randomChoices = BitArray.FromBinaryString("01011010 11001100 10101010 01011010 11001100 10101010");
            var rngMock = new Mock<RandomNumberGenerator>();
            rngMock.Setup(r => r.GetBytes(It.IsAny<byte[]>())).Callback((byte[] b) => {
                randomChoices.CopyTo(b);
            });


            var cryptoContext = new CryptoContext(
                rngMock.Object, SHA1.Create()
            );

            var otProtocol = new ALSZRandomObliviousTransferChannel(
                baseOTMock.Object, securityParameter.InBits, cryptoContext
            );

            otProtocol.ExecuteSenderBaseTransferAsync().Wait();

            rngMock.Verify(r => r.GetBytes(It.IsAny<byte[]>()), Times.AtLeastOnce());
            baseOTMock.Verify(ot => ot.ReceiveAsync(
                It.Is<BitSequence>(b => randomChoices.AsByteEnumerable().SequenceEqual(b.AsByteEnumerable())),
                It.Is<int>(i => i == securityParameter.InBits)), Times.Once());
        }

        [Fact]
        public void TestReceiverBaseOTs()
        {
            var securityParameter = NumberLength.FromBitLength(3);
            int codeLength = 2 * securityParameter.InBits;


            var baseOTMock = new Mock<ObliviousTransferChannel>();
            baseOTMock.Setup(ot => ot.SendAsync(It.IsAny<ObliviousTransferOptions>())).Returns(Task.CompletedTask);
            baseOTMock.Setup(ot => ot.SecurityLevel).Returns(1000000);

            var randomChoices = BitArray.FromBinaryString("00000000 01011010 11111111 11001100 1010");
            var rngMock = new Mock<RandomNumberGenerator>();
            rngMock.Setup(r => r.GetBytes(It.IsAny<byte[]>())).Callback((byte[] b) => {
                randomChoices.CopyTo(b);
            });

            var expectedOptions = ObliviousTransferOptions.FromBitArray(
                randomChoices, codeLength, 2, securityParameter.InBits
            );


            var cryptoContext = new CryptoContext(
                rngMock.Object, SHA1.Create()
            );

            var otProtocol = new ALSZRandomObliviousTransferChannel(
                baseOTMock.Object, securityParameter.InBits, cryptoContext
            );

            otProtocol.ExecuteReceiverBaseTransferAsync().Wait();

            rngMock.Verify(r => r.GetBytes(It.IsAny<byte[]>()), Times.AtLeastOnce());
            baseOTMock.Verify(ot => ot.SendAsync(
                It.Is<ObliviousTransferOptions>(o => o.Equals(expectedOptions))), Times.Once());
        }
    }
}
