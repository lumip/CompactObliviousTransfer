// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Linq;
using System.Text;
using System.Diagnostics;
using Xunit;

using System.Numerics;
using CompactCryptoGroupAlgebra.EllipticCurves;

namespace CompactOT
{

    public class NaorPinkasObliviousTransferChannelTests
    {

        [Fact]
        public void TestProtocolWithFullBytes()
        {
            int numberOfInvocations = 3;
            int numberOfOptions = TestUtils.TestOptions.Length;
            int numberOfMessageBits = TestUtils.TestOptions[0].Length * 8;

            // sender data
            var options = new ObliviousTransferOptions(numberOfInvocations, numberOfOptions, numberOfMessageBits);
            options.SetInvocation(0, TestUtils.TestOptions.Select(s => Encoding.ASCII.GetBytes(s)).ToArray());
            options.SetInvocation(1, TestUtils.TestOptions.Select(s => Encoding.ASCII.GetBytes(s.ToLower())).ToArray());
            options.SetInvocation(2, TestUtils.TestOptions.Select(s => Encoding.ASCII.GetBytes(s.ToUpper())).ToArray());

            // receiver data
            var receiverIndices = new int[] { 0, 5, 3 };

            TestRunner(options, receiverIndices);
        }

        [Fact]
        public void TestProtocolWithLessThanOneByte()
        {
            int numberOfInvocations = 2;
            int numberOfOptions = TestUtils.TestCorrelations.Length;
            int numberOfMessageBits = TestUtils.TestCorrelations[0].Length;

            // sender data
            var options = new ObliviousTransferOptions(numberOfInvocations, numberOfOptions, numberOfMessageBits);
            options.SetInvocation(0, TestUtils.TestCorrelations.ToArray());
            options.SetInvocation(1, TestUtils.TestCorrelations.Select(s => s.Not()).ToArray());

            // receiver data
            var receiverIndices = new int[] { 0, 3 };

            TestRunner(options, receiverIndices);
        }

        private void TestRunner(ObliviousTransferOptions options, int[] receiverIndices)
        {
            int numberOfInvocations = options.NumberOfInvocations;
            int numberOfOptions = options.NumberOfOptions;
            int numberOfMessageBits = options.NumberOfMessageBits;

            Debug.Assert(receiverIndices.Length == numberOfInvocations);

            var channels = new TestMessageChannels();
            var cryptoGroup = CurveGroupAlgebra.CreateCryptoGroup(securityLevel: 64);
            var cryptoContext = CryptoContext.CreateDefault();

            // protocol setup
            var senderOtChannel = new NaorPinkasObliviousTransferChannel<BigInteger, CurvePoint>(
                channels.FirstPartyChannel, cryptoGroup, cryptoContext
            );

            var receiverOtChannel = new NaorPinkasObliviousTransferChannel<BigInteger, CurvePoint>(
                channels.SecondPartyChannel, cryptoGroup, cryptoContext
            );

            // execute protocol
            var sendTask = senderOtChannel.SendAsync(options);
            var receiverTask = receiverOtChannel.ReceiveAsync(
                receiverIndices, numberOfOptions, numberOfMessageBits
            );

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
    }
}