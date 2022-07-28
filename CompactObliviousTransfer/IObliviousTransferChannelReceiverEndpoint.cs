using System.Threading.Tasks;

namespace CompactOT
{

    public interface IObliviousTransferChannelReceiverEndpoint
    {
        public Task<ObliviousTransferResult> ReceiveAsync(int[] selectionIndices, int numberOfOptions, int numberOfMessageBits);
        /// <summary>
        /// The network channel the OT operates on, uniquely identifying the pair of parties involved in the OT.
        /// </summary>
        public IMessageChannel Channel { get; }
        public int SecurityLevel { get; }
    }

}
