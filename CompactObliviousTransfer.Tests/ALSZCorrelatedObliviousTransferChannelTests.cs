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

        private ObliviousTransferChannel GetBaseTransferChannel(IMessageChannel messageChannel)
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

            var otProtocol = new ALSZCorrelatedObliviousTransferChannel(
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

            var otProtocol = new ALSZCorrelatedObliviousTransferChannel(
                baseOTMock.Object, securityParameter.InBits, cryptoContext
            );

            otProtocol.ExecuteReceiverBaseTransferAsync().Wait();

            rngMock.Verify(r => r.GetBytes(It.IsAny<byte[]>()), Times.AtLeastOnce());
            baseOTMock.Verify(ot => ot.SendAsync(
                It.Is<ObliviousTransferOptions>(o => o.Equals(expectedOptions))), Times.Once());
        }
    }
}
