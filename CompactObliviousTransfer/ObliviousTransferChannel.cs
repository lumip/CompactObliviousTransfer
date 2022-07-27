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
    /// 
    /// In a single invocation of 1-out-of-N Oblivious Transfer, the sender inputs
    /// N bit-strings x_0, ..., x_N and the receiver inputs a selection index s.
    /// The sender receives no outputs. The receiver receives as output the bit-string x_s.
    /// </summary>
    public abstract class ObliviousTransferChannel
    {
        public abstract Task SendAsync(ObliviousTransferOptions options);
        
        public abstract Task<ObliviousTransferResult> ReceiveAsync(int[] selectionIndices, int numberOfOptions, int numberOfMessageBits);

        public virtual Task<ObliviousTransferResult> ReceiveAsync(BitSequence selectionIndices, int numberOfMessageBits)
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
