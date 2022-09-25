using System.Threading.Tasks;

namespace CompactOT
{

    public interface IObliviousTransferChannelReceiverEndpoint
    {
        Task<ObliviousTransferResult> ReceiveAsync(int[] selectionIndices, int numberOfOptions, int numberOfMessageBits);

        /// <summary>
        /// The network channel the OT operates on, uniquely identifying the pair of parties involved in the OT.
        /// </summary>
        IMessageChannel Channel { get; }
        
        int SecurityLevel { get; }
    }

}
