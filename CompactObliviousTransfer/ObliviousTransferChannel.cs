using System.Threading.Tasks;
using System.Linq;

using CompactOT.DataStructures;

namespace CompactOT
{
    /// <summary>
    /// A 1-out-of-N Oblivious Transfer channel implementation.
    /// 
    /// Provides 1ooN-OT on a given channel (i.e., pair of parties) and may maintain
    /// channel-specific protocol state in-between invocations.
    /// </summary>
    public abstract class ObliviousTransferChannel
    {
        public abstract Task SendAsync(ObliviousTransferOptions options);
        
        public abstract Task<byte[][]> ReceiveAsync(int[] selectionIndices, int numberOfOptions, int numberOfMessageBits);

        public virtual Task<byte[][]> ReceiveAsync(BitArrayBase selectionIndices, int numberOfMessageBits)
        {
            return ReceiveAsync(
                selectionIndices.Select(x => x ? 1 : 0).ToArray(), 2, numberOfMessageBits
            );
        }

        /// <summary>
        /// The network channel the OT operates on, uniquely identifying the pair of parties involved in the OT.
        /// </summary>
        public abstract IMessageChannel Channel { get; }

        public abstract int SecurityLevel { get; }
    }
}
