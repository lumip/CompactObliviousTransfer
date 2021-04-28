using System.Linq;
using System.Text;
using Xunit;

namespace CompactOT
{

    public class InsecureObliviousTransferTests
    {
        private static readonly string[] TestOptions = { "Alicia", "Briann", "Charly", "Dennis", "Elenor", "Frieda" };

        [Fact]
        public void TestInsecureObliviousTransfer()
        {
            const int numberOfInvocations = 3;
            const int numberOfOptions = 6;
            int numberOfMessageBits = TestOptions[0].Length * 8;

            // sender data
            var options = new ObliviousTransferOptions(numberOfInvocations, numberOfOptions, numberOfMessageBits);

            options.SetInvocation(0, TestOptions.Select(s => Encoding.ASCII.GetBytes(s)).ToArray());
            options.SetInvocation(1, TestOptions.Select(s => Encoding.ASCII.GetBytes(s.ToLower())).ToArray());
            options.SetInvocation(2, TestOptions.Select(s => Encoding.ASCII.GetBytes(s.ToUpper())).ToArray());

            // receiver data
            var receiverIndices = new int[] { 0, 5, 3 };

            // protocol setup
            var otProtocol = new InsecureObliviousTransfer();
            var channels = new TestMessageChannels();

            // execute protocol
            var sendTask = otProtocol.SendAsync(channels.FirstPartyChannel, options);
            var receiverTask = otProtocol.ReceiveAsync(channels.SecondPartyChannel, receiverIndices, numberOfOptions, numberOfMessageBits);

            // verify results
            byte[][] results = receiverTask.Result;
            Assert.Equal(numberOfInvocations, results.Length);
            for (int i = 0; i < results.Length; ++i)
            {
                var expected = options.GetMessage(i, receiverIndices[i]);
                Assert.Equal(expected.ToBytes(), results[i]);
            }
        }
    }
}