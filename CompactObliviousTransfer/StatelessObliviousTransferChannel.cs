using System.Threading.Tasks;

using CompactOT.DataStructures;

namespace CompactOT
{
    public class StatelessObliviousTransferChannel : ObliviousTransferChannel
    {

        private StatelessObliviousTransfer _statelessOT;
        public override IMessageChannel Channel { get; }

        /// <inheritdoc/>
        public override int SecurityLevel => _statelessOT.SecurityLevel;

        public StatelessObliviousTransferChannel(StatelessObliviousTransfer statelessObliviousTransfer, IMessageChannel channel)
        {
            _statelessOT = statelessObliviousTransfer;
            Channel = channel;
        }

        /// <inheritdoc/>
        public override Task<byte[][]> ReceiveAsync(int[] selectionIndices, int numberOfOptions, int numberOfMessageBits)
        {
            return _statelessOT.ReceiveAsync(Channel, selectionIndices, numberOfOptions, numberOfMessageBits);
        }

        /// <inheritdoc/>
        public override Task<byte[][]> ReceiveAsync(BitArray selectionIndices, int numberOfMessageBits)
        {
            return _statelessOT.ReceiveAsync(Channel, selectionIndices, numberOfMessageBits);
        }

        /// <inheritdoc/>
        public override Task SendAsync(ObliviousTransferOptions<byte> options)
        {
            return _statelessOT.SendAsync(Channel, options);
        }
    }
}