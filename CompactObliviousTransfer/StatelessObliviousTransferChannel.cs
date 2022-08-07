using System.Threading.Tasks;

namespace CompactOT
{
    public class StatelessObliviousTransferChannel : IObliviousTransferChannel
    {

        private IStatelessObliviousTransfer _statelessOT;

        /// <inheritdoc/>
        public IMessageChannel Channel { get; }

        /// <inheritdoc/>
        public int SecurityLevel => _statelessOT.SecurityLevel;

        public StatelessObliviousTransferChannel(IStatelessObliviousTransfer statelessObliviousTransfer, IMessageChannel channel)
        {
            _statelessOT = statelessObliviousTransfer;
            Channel = channel;
        }

        /// <inheritdoc/>
        public Task<ObliviousTransferResult> ReceiveAsync(int[] selectionIndices, int numberOfOptions, int numberOfMessageBits)
        {
            return _statelessOT.ReceiveAsync(Channel, selectionIndices, numberOfOptions, numberOfMessageBits);
        }

        /// <inheritdoc/>
        public Task SendAsync(ObliviousTransferOptions options)
        {
            return _statelessOT.SendAsync(Channel, options);
        }

        public double EstimateCost(ObliviousTransferUsageProjection usageProjection)
        {
            return _statelessOT.EstimateCost(usageProjection);
        }
    }
}
