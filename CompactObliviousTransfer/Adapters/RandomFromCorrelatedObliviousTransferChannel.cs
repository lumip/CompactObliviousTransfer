using System.Threading.Tasks;
using System.Security.Cryptography;

namespace CompactOT.Adapters
{

    public class RandomFromCorrelatedObliviousTransferChannel : IRandomObliviousTransferChannel
    {
        private ICorrelatedObliviousTransferChannel _cotChannel;
        private ThreadsafeRandomNumberGenerator _randomNumberGenerator;

        public RandomFromCorrelatedObliviousTransferChannel(
            ICorrelatedObliviousTransferChannel correlatedObliviousTransferChannel, RandomNumberGenerator randomNumberGenerator
        )
        {
            _cotChannel = correlatedObliviousTransferChannel;
            _randomNumberGenerator = new ThreadsafeRandomNumberGenerator(randomNumberGenerator);
        }

        public IMessageChannel Channel => _cotChannel.Channel;

        public int SecurityLevel => _cotChannel.SecurityLevel;

        public Task<ObliviousTransferResult> ReceiveAsync(int[] selectionIndices, int numberOfOptions, int numberOfMessageBits)
        {
            return _cotChannel.ReceiveAsync(selectionIndices, numberOfOptions, numberOfMessageBits);
        }

        public async Task<ObliviousTransferOptions> SendAsync(int numberOfInvocations, int numberOfOptions, int numberOfMessageBits)
        {
            var correlations = new ObliviousTransferOptions(
                numberOfInvocations, numberOfOptions - 1, numberOfMessageBits
            );
            for (int i = 0; i < numberOfInvocations; ++i)
            {
                for (int j = 0; j < numberOfOptions - 1; ++j)
                {
                    var correlation = _randomNumberGenerator.GetBits(numberOfMessageBits);
                    correlations.SetMessage(i, j, correlation);      
                }
            }
            ObliviousTransferResult firstOptions = await _cotChannel.SendAsync(correlations);
            return ObliviousTransferOptions.FromCorrelatedTransfer(firstOptions, correlations);
        }

        public double EstimateCost(ObliviousTransferUsageProjection usageProjection)
        {
            return _cotChannel.EstimateCost(usageProjection);
        }

    }
}
