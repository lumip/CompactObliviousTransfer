using System.Linq;
using System.Text;
using Xunit;
using Moq;

using System.Numerics;

using CompactOT.DataStructures;

namespace CompactOT
{

    public class NaorPinkasObliviousTransferTests
    {
        private static readonly string[] TestOptions = { "Alicia", "Briann", "Charly", "Dennis", "Elenor", "Frieda" };

        [Fact]
        public void TestNaorPinkasObliviousTransfer()
        {
            const int numberOfInvocations = 3;
            int numberOfOptions = TestOptions.Length;
            int numberOfMessageBits = TestOptions[0].Length * 8;

            // sender data
            var options = new ObliviousTransferOptions(numberOfInvocations, numberOfOptions, numberOfMessageBits);

            options.SetInvocation(0, TestOptions.Select(s => Encoding.ASCII.GetBytes(s)).ToArray());
            options.SetInvocation(1, TestOptions.Select(s => Encoding.ASCII.GetBytes(s.ToLower())).ToArray());
            options.SetInvocation(2, TestOptions.Select(s => Encoding.ASCII.GetBytes(s.ToUpper())).ToArray());

            // receiver data
            var receiverIndices = new int[] { 0, 5, 3 };

            // protocol setup
            var otProtocol = new NaorPinkasObliviousTransfer<BigInteger, BigInteger>(
                CryptoGroupDefaults.Create768BitMultiplicativeGroup(),
                CryptoContext.CreateDefault()
            );
            var channels = new TestMessageChannels();

            // execute protocol
            var sendTask = otProtocol.SendAsync(channels.FirstPartyChannel, options);
            var receiverTask = otProtocol.ReceiveAsync(channels.SecondPartyChannel, receiverIndices, numberOfOptions, numberOfMessageBits);

            TestUtils.WaitAllOrFail(sendTask, receiverTask);

            // verify results
            BitMatrix results = receiverTask.Result;
            Assert.Equal(numberOfInvocations, results.Rows);
            for (int i = 0; i < results.Rows; ++i)
            {
                var expected = options.GetMessage(i, receiverIndices[i]);
                Assert.Equal(expected, results.GetRow(i));
            }
        }
    }
}