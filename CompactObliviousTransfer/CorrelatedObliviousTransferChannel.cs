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
    /// In a single invocation of 1-out-of-N Correlated Oblivious Transfer, the sender inputs
    /// a set of of (N-1) correlation bit-strings c_1, ..., c_N and the receiver inputs a
    /// selection index s.
    /// The sender receives as output a random bit-string x_0. The receiver receives as output
    /// the bit-string x_s = x_0 ^ c_s (with c_0 = 0).
    /// </summary>
    public abstract class CorrelatedObliviousTransferChannel
    {
        public abstract Task<ObliviousTransferResult> SendAsync(ObliviousTransferOptions options);
        
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
