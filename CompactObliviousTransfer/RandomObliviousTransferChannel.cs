using System.Threading.Tasks;
using System.Linq;

using CompactOT.DataStructures;

namespace CompactOT
{
    /// <summary>
    /// A 1-out-of-N Correlated Oblivious Transfer channel implementation.
    /// 
    /// Provides 1ooN-COT on a given channel (i.e., pair of parties) and may maintain
    /// channel-specific protocol state in-between invocations.
    /// 
    /// In a single invocation of 1-out-of-N Random Oblivious Transfer, the sender inputs
    /// nothing and the receiver inputs a selection index s.
    /// The sender receives as output N random bit-strings x_0, ..., X_N. The receiver receives
    /// as output the bit-string x_s.
    /// </summary>
    public abstract class RandomObliviousTransferChannel
    {
        public abstract Task<ObliviousTransferOptions> SendAsync(int numberOfInvocations, int numberOfOptions, int numberOfMessageBits);
        
        public abstract Task<BitMatrix> ReceiveAsync(int[] selectionIndices, int numberOfOptions, int numberOfMessageBits);

        public virtual Task<BitMatrix> ReceiveAsync(BitSequence selectionIndices, int numberOfMessageBits)
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
