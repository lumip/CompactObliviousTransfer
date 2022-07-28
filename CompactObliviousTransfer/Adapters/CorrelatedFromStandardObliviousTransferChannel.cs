using System.Threading.Tasks;
using System.Security.Cryptography;

namespace CompactOT.Adapters
{

    public class CorrelatedFromStandardObliviousTransferChannel : ICorrelatedObliviousTransferChannel
    {
        private IObliviousTransferChannel _otChannel;
        private ThreadsafeRandomNumberGenerator _randomNumberGenerator;

        public CorrelatedFromStandardObliviousTransferChannel(
            IObliviousTransferChannel obliviousTransferChannel, RandomNumberGenerator randomNumberGenerator
        )
        {
            _otChannel = obliviousTransferChannel;
            _randomNumberGenerator = new ThreadsafeRandomNumberGenerator(randomNumberGenerator);
        }

        public IMessageChannel Channel => _otChannel.Channel;

        public int SecurityLevel => _otChannel.SecurityLevel;

        public Task<ObliviousTransferResult> ReceiveAsync(int[] selectionIndices, int numberOfOptions, int numberOfMessageBits)
        {
            return _otChannel.ReceiveAsync(selectionIndices, numberOfOptions, numberOfMessageBits);
        }

        public Task<ObliviousTransferResult> SendAsync(ObliviousTransferOptions correlations)
        {
            var firstOptions = new ObliviousTransferResult(correlations.NumberOfInvocations, correlations.NumberOfMessageBits);
            var options = new ObliviousTransferOptions(
                correlations.NumberOfInvocations, correlations.NumberOfOptions + 1, correlations.NumberOfMessageBits
            );
            for (int i = 0; i < correlations.NumberOfInvocations; ++i)
            {
                var firstOption = _randomNumberGenerator.GetBits(correlations.NumberOfMessageBits);
                firstOptions.SetRow(i, firstOption);
                options.SetMessage(i, 0, firstOption);
                for (int j = 0; j < correlations.NumberOfOptions; ++j)
                {
                    options.SetMessage(i, j + 1, correlations.GetMessage(i, j) ^ firstOption);
                }
            }
            return _otChannel.SendAsync(options).ContinueWith(t => firstOptions);
        }
    }
}
