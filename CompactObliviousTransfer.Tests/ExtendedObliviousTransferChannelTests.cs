using System;
using Xunit;
using Moq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Linq;
using System.Text;

using CompactCryptoGroupAlgebra;
using CompactOT.DataStructures;

namespace CompactOT
{
    public class ExtendedObliviousTransferChannelTests
    {

        private ObliviousTransferChannel GetBaseTransferChannel(IMessageChannel messageChannel)
        {
            var otMock = new Mock<InsecureObliviousTransfer>() { CallBase = true };
            otMock.Setup(ot => ot.SecurityLevel).Returns(10000000);
            return new StatelessObliviousTransferChannel(otMock.Object, messageChannel);
        }

        [Fact]
        public void TestBaseOTs()
        {
            var securityParameter = NumberLength.FromBitLength(24);

            var messageChannels = new TestMessageChannels();
            var senderBaseChannel = GetBaseTransferChannel(messageChannels.FirstPartyChannel);
            var receiverBaseChannel = GetBaseTransferChannel(messageChannels.SecondPartyChannel);

            var cryptoContext = CryptoContext.CreateDefault();
            var otSender = new ExtendedObliviousTransferChannel(senderBaseChannel, securityParameter.InBits, cryptoContext);
            var otReceiver = new ExtendedObliviousTransferChannel(receiverBaseChannel, securityParameter.InBits, cryptoContext);
            
            var senderTask = otSender.ExecuteSenderBaseTransferAsync();
            var receiverTask = otReceiver.ExecuteReceiverBaseTransferAsync();

            TestUtils.WaitAllOrFail(senderTask, receiverTask);
        }

        private static readonly string[] TestOptions = { "Alicia", "Briann", "Charly", "Dennis", "Elenor", "Frieda" };

        [Fact]
        public void TestExtendedOTs()
        {
            int numberOfOptions = TestOptions.Length;

            var securityParameter = NumberLength.FromBitLength(8);

            var messageChannels = new TestMessageChannels();
            var senderBaseChannel = GetBaseTransferChannel(messageChannels.FirstPartyChannel);
            var receiverBaseChannel = GetBaseTransferChannel(messageChannels.SecondPartyChannel);

            var cryptoContext = CryptoContext.CreateDefault();
            var otSender = new ExtendedObliviousTransferChannel(senderBaseChannel, securityParameter.InBits, cryptoContext);
            var otReceiver = new ExtendedObliviousTransferChannel(receiverBaseChannel, securityParameter.InBits, cryptoContext);

            const int numberOfInvocations = 3;
            int numberOfMessageBits = TestOptions[0].Length * 8;

            // sender data
            var options = new ObliviousTransferOptions(numberOfInvocations, numberOfOptions, numberOfMessageBits);

            options.SetInvocation(0, TestOptions.Select(s => Encoding.ASCII.GetBytes(s)).ToArray());
            options.SetInvocation(1, TestOptions.Select(s => Encoding.ASCII.GetBytes(s.ToLower())).ToArray());
            options.SetInvocation(2, TestOptions.Select(s => Encoding.ASCII.GetBytes(s.ToUpper())).ToArray());

            // receiver data
            var receiverIndices = new int[] { 0, 5, 3 };

            // execute protocol
            var sendTask = otSender.SendAsync(options);
            var receiverTask = otReceiver.ReceiveAsync(receiverIndices, numberOfOptions, numberOfMessageBits);

            TestUtils.WaitAllOrFail(sendTask, receiverTask);

            // verify results
            ObliviousTransferResult results = receiverTask.Result;
            Assert.Equal(numberOfInvocations, results.NumberOfInvocations);
            Assert.Equal(numberOfMessageBits, results.NumberOfMessageBits);
            for (int i = 0; i < results.NumberOfInvocations; ++i)
            {
                var expected = options.GetMessage(i, receiverIndices[i]);
                Assert.Equal(expected, results.GetInvocationResult(i));
            }
        }

        [Fact]
        public void TestSenderBaseOTs()
        {
            var securityParameter = NumberLength.FromBitLength(24);
            int codeLength = 2 * securityParameter.InBits;

            ObliviousTransferResult received = new ObliviousTransferResult(codeLength, securityParameter.InBits);
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

            var otProtocol = new ExtendedObliviousTransferChannel(
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

            var otProtocol = new ExtendedObliviousTransferChannel(
                baseOTMock.Object, securityParameter.InBits, cryptoContext
            );

            otProtocol.ExecuteReceiverBaseTransferAsync().Wait();

            rngMock.Verify(r => r.GetBytes(It.IsAny<byte[]>()), Times.AtLeastOnce());
            baseOTMock.Verify(ot => ot.SendAsync(
                It.Is<ObliviousTransferOptions>(o => o.Equals(expectedOptions))), Times.Once());
        }
    }
}
