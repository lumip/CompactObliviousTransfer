// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace CompactOT
{

    public class InsecureObliviousTransferChannelTests
    {

        [Fact]
        public void TestInsecureObliviousTransferWithFullBytes()
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
        public void TestInsecureObliviousTransferWithLessThanOneByte()
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

        [Fact]
        public async void TestInsecureObliviousTransferWithCancellation()
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

            var tokenSource = new CancellationTokenSource();
            var cancellationToken = tokenSource.Token;
            tokenSource.Cancel();

            // protocol setup
            var channels = new TestMessageChannels();
            var senderOtChannel = new InsecureObliviousTransferChannel(channels.FirstPartyChannel);
            var receiverOtChannel = new InsecureObliviousTransferChannel(channels.SecondPartyChannel);

            // execute protocol
            var sendTask = senderOtChannel.SendAsync(options, cancellationToken);
            var receiverTask = receiverOtChannel.ReceiveAsync(receiverIndices, numberOfOptions, numberOfMessageBits, cancellationToken);

            await TestUtils.WhenAllOrFail(sendTask, receiverTask);

            Assert.True(sendTask.IsCanceled);
            Assert.True(receiverTask.IsCanceled);
        }

        private async static void TestRunner(ObliviousTransferOptions options, int[] receiverIndices)
        {
            int numberOfInvocations = options.NumberOfInvocations;
            int numberOfOptions = options.NumberOfOptions;
            int numberOfMessageBits = options.NumberOfMessageBits;

            Debug.Assert(receiverIndices.Length == numberOfInvocations);

            // protocol setup
            var channels = new TestMessageChannels();
            IObliviousTransferChannel senderOtChannel = new InsecureObliviousTransferChannel(channels.FirstPartyChannel);
            IObliviousTransferChannelReceiverEndpoint receiverOtChannel = new InsecureObliviousTransferChannel(channels.SecondPartyChannel);

            var tokenSource = new CancellationTokenSource(TestUtils.TestTimeoutMs);

            // execute protocol
            var sendTask = senderOtChannel.SendAsync(options, tokenSource.Token);
            var receiverTask = receiverOtChannel.ReceiveAsync(receiverIndices, numberOfOptions, numberOfMessageBits, tokenSource.Token);

            await TestUtils.WhenAllOrFail(sendTask, receiverTask);

            // verify results
            ObliviousTransferResult results = await receiverTask;
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
