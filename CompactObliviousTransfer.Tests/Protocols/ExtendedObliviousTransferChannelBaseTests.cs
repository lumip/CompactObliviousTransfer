// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using Xunit;
using Moq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Linq;

using CompactCryptoGroupAlgebra;
using CompactOT.DataStructures;
using CompactOT.Codes;

namespace CompactOT
{
    public class ExtendedObliviousTransferChannelBaseTests
    {

        [Fact]
        public void TestConstructorAndProperties()
        {
            int securityLevel = 128;

            var channelMock = new Mock<IMessageChannel>();
            var baseOtMock = new Mock<IObliviousTransferChannel>();
            baseOtMock.Setup(ot => ot.SecurityLevel).Returns(securityLevel);
            baseOtMock.Setup(ot => ot.Channel).Returns(channelMock.Object);

            var codeMock = new Mock<IBinaryCode>();
            codeMock.Setup(c => c.Distance).Returns(securityLevel);

            var cryptoContext = CryptoContext.CreateDefault();

            var otChannel = new ExtendedObliviousTransferChannelBase(baseOtMock.Object, securityLevel, cryptoContext, codeMock.Object);

            Assert.Same(channelMock.Object, otChannel.Channel);
            Assert.Equal(securityLevel, otChannel.SecurityLevel);
            Assert.Equal(0, otChannel.TotalNumberOfInvocations);
        }

        [Fact]
        public void TestConstructorRejectsBadSecurityLevel()
        {
            int securityLevel = 0;

            var baseOtMock = new Mock<IObliviousTransferChannel>();
            var codeMock = new Mock<IBinaryCode>();
            var cryptoContext = CryptoContext.CreateDefault();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ExtendedObliviousTransferChannelBase(baseOtMock.Object, securityLevel, cryptoContext, codeMock.Object)
            );
        }

        [Fact]
        public void TestConstructorRejectsBadBaseOT()
        {
            int securityLevel = 32;

            var baseOtMock = new Mock<IObliviousTransferChannel>();
            baseOtMock.Setup(ot => ot.SecurityLevel).Returns(30);

            var codeMock = new Mock<IBinaryCode>();
            var cryptoContext = CryptoContext.CreateDefault();

            Assert.Throws<ArgumentException>(
                () => new ExtendedObliviousTransferChannelBase(baseOtMock.Object, securityLevel, cryptoContext, codeMock.Object)
            );
        }

        [Fact]
        public void TestConstructorRejectsBadCode()
        {
            int securityLevel = 128;

            var channelMock = new Mock<IMessageChannel>();
            var baseOtMock = new Mock<IObliviousTransferChannel>();
            baseOtMock.Setup(ot => ot.SecurityLevel).Returns(securityLevel);
            baseOtMock.Setup(ot => ot.Channel).Returns(channelMock.Object);

            int codeLength = 2*securityLevel;
            var codeMock = new Mock<IBinaryCode>();
            codeMock.Setup(c => c.CodeLength).Returns(codeLength);
            codeMock.Setup(c => c.Distance).Returns(32);

            var cryptoContext = CryptoContext.CreateDefault();

            Assert.Throws<ArgumentException>(
                () => new ExtendedObliviousTransferChannelBase(baseOtMock.Object, securityLevel, cryptoContext, codeMock.Object)
            );
        }

        [Fact]
        public void TestBaseOTs()
        {
            int securityLevel = 24;

            var messageChannels = new TestMessageChannels();
            var senderBaseChannel = TestUtils.GetBaseTransferChannel(messageChannels.FirstPartyChannel);
            var receiverBaseChannel = TestUtils.GetBaseTransferChannel(messageChannels.SecondPartyChannel);

            var cryptoContext = CryptoContext.CreateDefault();
            var code = WalshHadamardCode.CreateWithDistance(cryptoContext.SecurityLevel);

            var otSender = new ExtendedObliviousTransferChannelBase(senderBaseChannel, securityLevel, cryptoContext, code);
            var otReceiver = new ExtendedObliviousTransferChannelBase(receiverBaseChannel, securityLevel, cryptoContext, code);
            
            var senderTask = otSender.ExecuteSenderBaseTransferAsync();
            var receiverTask = otReceiver.ExecuteReceiverBaseTransferAsync();

            TestUtils.WaitAllOrFail(senderTask, receiverTask);
        }

        [Fact]
        public async void TestSenderBaseOTs()
        {
            var securityLevel = NumberLength.FromBitLength(16);
            var code = WalshHadamardCode.CreateWithDistance(securityLevel.InBits);
            int codeLength = code.CodeLength;

            var received = new ObliviousTransferResult(codeLength, securityLevel.InBits);
            for (int j = 0; j < codeLength; ++j)
            {
                byte[] receivedAsBytes = new byte[securityLevel.InBytes];
                for (int i = 0; i < securityLevel.InBytes; ++i)
                {
                    receivedAsBytes[i] = (byte)(j*10 + i);
                }
                received.SetRow(j, new EnumeratedBitArrayView(receivedAsBytes, securityLevel.InBits));
            }

            var baseOTMock = new Mock<IObliviousTransferChannel>();
            baseOTMock.Setup(ot => ot.ReceiveAsync(It.IsAny<int[]>(), It.Is<int>(o => o == 2), It.IsAny<int>()))
                .Returns(Task.FromResult(received));
            baseOTMock.Setup(ot => ot.SecurityLevel).Returns(1000000);

            var randomChoices = BitArray.FromBinaryString("01011010 11001100 10101010 01011010");
            var rngMock = new Mock<RandomNumberGenerator>();
            rngMock.Setup(r => r.GetBytes(It.IsAny<byte[]>())).Callback((byte[] b) => {
                randomChoices.CopyTo(b);
            });


            var cryptoContext = new CryptoContext(
                rngMock.Object, new SHA256Provider()
            );

            var otProtocol = new ExtendedObliviousTransferChannelBase(
                baseOTMock.Object, securityLevel.InBits, cryptoContext, code
            );

            await otProtocol.ExecuteSenderBaseTransferAsync();

            rngMock.Verify(r => r.GetBytes(It.IsAny<byte[]>()), Times.AtLeastOnce());
            baseOTMock.Verify(ot => ot.ReceiveAsync(
                It.Is<int[]>(b => randomChoices.ToSelectionIndices().SequenceEqual(b)),
                It.Is<int>(o => o == 2),
                It.Is<int>(i => i == securityLevel.InBits)), Times.Once());
        }

        [Fact]
        public async void TestSenderBaseOTsGetReplyWithWrongNumberOfInvocations()
        {
            var securityLevel = NumberLength.FromBitLength(16);
            var code = WalshHadamardCode.CreateWithDistance(securityLevel.InBits);
            int numberOfInvocationsInResponse = 2;

            var received = new ObliviousTransferResult(numberOfInvocationsInResponse, securityLevel.InBits);
            for (int j = 0; j < numberOfInvocationsInResponse; ++j)
            {
                byte[] receivedAsBytes = new byte[securityLevel.InBytes];
                for (int i = 0; i < securityLevel.InBytes; ++i)
                {
                    receivedAsBytes[i] = (byte)(j*10 + i);
                }
                received.SetRow(j, new EnumeratedBitArrayView(receivedAsBytes, securityLevel.InBits));
            }

            var baseOTMock = new Mock<IObliviousTransferChannel>();
            baseOTMock.Setup(ot => ot.ReceiveAsync(It.IsAny<int[]>(), It.Is<int>(o => o == 2), It.IsAny<int>()))
                .Returns(Task.FromResult(received));
            baseOTMock.Setup(ot => ot.SecurityLevel).Returns(1000000);

            var randomChoices = BitArray.FromBinaryString("01011010 11001100 10101010 01011010");
            var rngMock = new Mock<RandomNumberGenerator>();
            rngMock.Setup(r => r.GetBytes(It.IsAny<byte[]>())).Callback((byte[] b) => {
                randomChoices.CopyTo(b);
            });

            var cryptoContext = CryptoContext.CreateDefault();

            var otProtocol = new ExtendedObliviousTransferChannelBase(
                baseOTMock.Object, securityLevel.InBits, cryptoContext, code
            );

            await Assert.ThrowsAsync<ProtocolException>(
                async () => await otProtocol.ExecuteSenderBaseTransferAsync()
            );
        }

        [Fact]
        public async void TestSenderBaseOTsGetReplyWithWrongNumberOfMessageBits()
        {
            int securityLevel = 16;
            var code = WalshHadamardCode.CreateWithDistance(securityLevel);
            int numberOfInvocationsInResponse = code.CodeLength;
            var messageLength = NumberLength.FromBitLength(2);

            var received = new ObliviousTransferResult(numberOfInvocationsInResponse, messageLength.InBits);
            for (int j = 0; j < numberOfInvocationsInResponse; ++j)
            {
                byte[] receivedAsBytes = new byte[messageLength.InBytes];
                for (int i = 0; i < messageLength.InBytes; ++i)
                {
                    receivedAsBytes[i] = (byte)(j*10 + i);
                }
                received.SetRow(j, new EnumeratedBitArrayView(receivedAsBytes, messageLength.InBits));
            }

            var baseOTMock = new Mock<IObliviousTransferChannel>();
            baseOTMock.Setup(ot => ot.ReceiveAsync(It.IsAny<int[]>(), It.Is<int>(o => o == 2), It.IsAny<int>()))
                .Returns(Task.FromResult(received));
            baseOTMock.Setup(ot => ot.SecurityLevel).Returns(1000000);

            var randomChoices = BitArray.FromBinaryString("01011010 11001100 10101010 01011010");
            var rngMock = new Mock<RandomNumberGenerator>();
            rngMock.Setup(r => r.GetBytes(It.IsAny<byte[]>())).Callback((byte[] b) => {
                randomChoices.CopyTo(b);
            });

            var cryptoContext = CryptoContext.CreateDefault();

            var otProtocol = new ExtendedObliviousTransferChannelBase(
                baseOTMock.Object, securityLevel, cryptoContext, code
            );

            await Assert.ThrowsAsync<ProtocolException>(
                async () => await otProtocol.ExecuteSenderBaseTransferAsync()
            );
        }

        [Fact]
        public async void TestReceiverBaseOTs()
        {
            int securityLevel = 4;
            var code = WalshHadamardCode.CreateWithDistance(securityLevel);
            int codeLength = code.CodeLength;

            var baseOTMock = new Mock<IObliviousTransferChannel>();
            baseOTMock.Setup(ot => ot.SendAsync(It.IsAny<ObliviousTransferOptions>())).Returns(Task.CompletedTask);
            baseOTMock.Setup(ot => ot.SecurityLevel).Returns(1000000);

            var randomChoices = BitArray.FromBinaryString("00000000 01011010 11111111 11001100 10100101 00101101 10010110 01010101");
            var rngMock = new Mock<RandomNumberGenerator>();
            rngMock.Setup(r => r.GetBytes(It.IsAny<byte[]>())).Callback((byte[] b) => {
                randomChoices.CopyTo(b);
            });

            var cryptoContext = new CryptoContext(
                rngMock.Object, new SHA256Provider()
            );

            var expectedOptions = ObliviousTransferOptions.FromBitArray(
                randomChoices, codeLength, 2, securityLevel
            );

            var otProtocol = new ExtendedObliviousTransferChannelBase(
                baseOTMock.Object, securityLevel, cryptoContext, code
            );

            await otProtocol.ExecuteReceiverBaseTransferAsync();

            rngMock.Verify(r => r.GetBytes(It.IsAny<byte[]>()), Times.AtLeastOnce());
            baseOTMock.Verify(ot => ot.SendAsync(
                It.Is<ObliviousTransferOptions>(o => o.Equals(expectedOptions))), Times.Once());
        }

        [Fact]
        public void TestEstimateCostNoMaxNumberOfInvocations()
        {
            int securityLevel = 4;

            var baseOTMock = new Mock<IObliviousTransferChannel>();
            baseOTMock.Setup(ot => ot.SecurityLevel).Returns(1000000);

            var cryptoContext = CryptoContext.CreateDefault();
            var code = WalshHadamardCode.CreateWithDistance(securityLevel);

            var otProtocol = new ExtendedObliviousTransferChannelBase(
                baseOTMock.Object, securityLevel, cryptoContext, code
            );

            var usageProjection = new ObliviousTransferUsageProjection();
            Assert.True(double.IsPositiveInfinity(otProtocol.EstimateCost(usageProjection)));
        }

        [Fact]
        public void TestEstimateCost()
        {
            int securityLevel = 4;

            var baseOTMock = new Mock<IObliviousTransferChannel>();
            baseOTMock.Setup(ot => ot.SecurityLevel).Returns(1000000);
            double baseCost = 3.5;
            baseOTMock.Setup(ot => ot.EstimateCost(It.IsAny<ObliviousTransferUsageProjection>())).Returns(baseCost);

            var cryptoContext = CryptoContext.CreateDefault();

            int codeLength = 15;
            var codeMock = new Mock<IBinaryCode>();
            codeMock.Setup(c => c.CodeLength).Returns(codeLength);
            codeMock.Setup(c => c.Distance).Returns(securityLevel);
            var code = codeMock.Object;

            var otProtocol = new ExtendedObliviousTransferChannelBase(
                baseOTMock.Object, securityLevel, cryptoContext, code
            );

            var usageProjection = new ObliviousTransferUsageProjection
            {
                MaxNumberOfInvocations = 7,
                AverageNumberOfOptions = 3,
                AverageMessageBits = 11,
            };

            double expectedCost = baseCost +
                usageProjection.MaxNumberOfBatches * usageProjection.AverageInvocationsPerBatch * codeLength;

            double actualCost = otProtocol.EstimateCost(usageProjection);

            Assert.Equal(expectedCost, actualCost);

            var baseOtUsageProjection = new ObliviousTransferUsageProjection
            {
                MaxNumberOfInvocations = code.CodeLength,
                MaxNumberOfOptions = 2,
                MaxNumberOfBatches = 1,
                AverageMessageBits = securityLevel,
            };

            baseOTMock.Verify(
                ot => ot.EstimateCost(It.Is<ObliviousTransferUsageProjection>(o => o.Equals(baseOtUsageProjection))),
                Times.Once
            );
        }

    }
}
