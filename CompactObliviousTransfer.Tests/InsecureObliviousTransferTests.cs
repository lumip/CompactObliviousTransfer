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
            int numberOfMessageBytes = TestOptions[0].Length;

            // sender data
            var options = new ObliviousTransferOptions<byte>(numberOfInvocations, numberOfOptions, numberOfMessageBytes);

            options.SetInvocationOptions(0, TestOptions.Select(s => Encoding.ASCII.GetBytes(s)));
            options.SetInvocationOptions(1, TestOptions.Select(s => Encoding.ASCII.GetBytes(s.ToLower())));
            options.SetInvocationOptions(2, TestOptions.Select(s => Encoding.ASCII.GetBytes(s.ToUpper())));

            // receiver data
            var receiverIndices = new int[] { 0, 5, 3 };

            // protocol setup
            var otProtocol = new InsecureObliviousTransfer();
            var channels = new TestMessageChannels();

            // execute protocol
            var sendTask = otProtocol.SendAsync(channels.FirstPartyChannel, options);
            var receiverTask = otProtocol.ReceiveAsync(channels.SecondPartyChannel, receiverIndices, numberOfOptions, numberOfMessageBytes * 8);

            // verify results
            byte[][] results = receiverTask.Result;
            Assert.Equal(numberOfInvocations, results.Length);
            for (int i = 0; i < results.Length; ++i)
            {
                var expected = options.GetMessageOption(i, receiverIndices[i]);
                Assert.Equal(expected, results[i]);
            }
        }
    }
}