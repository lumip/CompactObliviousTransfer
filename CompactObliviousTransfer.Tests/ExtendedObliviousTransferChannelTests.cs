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
    public class ExtendedObliviousTransferChannelTests
    {

        [Fact]
        public void TestBaseOTs()
        {
            int numberOfOptions = 6;
            var securityParameter = NumberLength.FromBitLength(24);

            var messageChannels = new TestMessageChannels();
            var senderBaseChannel = new StatelessObliviousTransferChannel(new InsecureObliviousTransfer(), messageChannels.FirstPartyChannel);
            var receiverBaseChannel = new StatelessObliviousTransferChannel(new InsecureObliviousTransfer(), messageChannels.SecondPartyChannel);

            var cryptoContext = CryptoContext.CreateDefault();
            var otSender = new ExtendedObliviousTransferChannel(senderBaseChannel, numberOfOptions, securityParameter.InBits, cryptoContext);
            var otReceiver = new ExtendedObliviousTransferChannel(receiverBaseChannel, numberOfOptions, securityParameter.InBits, cryptoContext);
            
            var senderTask = otSender.ExecuteSenderBaseOTAsync();
            var receiverTask = otReceiver.ExecuteReceiverBaseOTAsync();

            Task.WaitAll(senderTask, receiverTask);
        }

        [Fact]
        public void TestSenderBaseOTs()
        {
            int numberOfOptions = 6;
            var securityParameter = NumberLength.FromBitLength(24);
            int codeLength = 2 * securityParameter.InBits;

            byte[][] received = new byte[codeLength][];
            for (int j = 0; j < received.Length; ++j)
            {
                received[j] = new byte[securityParameter.InBytes];
                for (int i = 0; i < securityParameter.InBytes; ++i)
                {
                    received[j][i] = (byte)(j*10 + i);
                }
            }

            var baseOTMock = new Mock<ObliviousTransferChannel>();
            baseOTMock.Setup(ot => ot.ReceiveAsync(It.IsAny<BitArray>(), It.IsAny<int>()))
                .Returns(Task.FromResult(received));

            // var randomCoiceBuffer = new byte[] { 0x5A, 0x33, 0x55, 0x5A, 0x33, 0x55 };
            var randomChoices = BitArray.FromBinaryString("01011010 11001100 10101010 01011010 11001100 10101010");
            var rngMock = new Mock<RandomNumberGenerator>();
            rngMock.Setup(r => r.GetBytes(It.IsAny<byte[]>())).Callback((byte[] b) => {
                randomChoices.CopyTo(b, 0);
            });


            var cryptoContext = new CryptoContext(
                rngMock.Object, SHA1.Create()
            );

            var otProtocol = new ExtendedObliviousTransferChannel(
                baseOTMock.Object, numberOfOptions, securityParameter.InBits, cryptoContext
            );

            otProtocol.ExecuteSenderBaseOTAsync().Wait();

            rngMock.Verify(r => r.GetBytes(It.IsAny<byte[]>()), Times.AtLeastOnce());
            baseOTMock.Verify(ot => ot.ReceiveAsync(
                It.Is<BitArrayBase>(b => randomChoices.AsByteEnumerable().SequenceEqual(b.AsByteEnumerable())),
                It.Is<int>(i => i == securityParameter.InBits)), Times.Once());

        }
    }
}
