using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

using CompactOT.DataStructures;

namespace CompactOT
{
    /// <summary>
    /// A stateless 1-out-of-N Oblivious Transfer implementation.
    /// 
    /// Stateless here means that the OT implementation does not maintain state for each channel (i.e., pair of communicating parties)
    /// in-between invocations.
    /// </summary>
    public abstract class StatelessObliviousTransfer
    {
        /// <summary>
        /// Starts a K-fold 1-out-of-N Oblivious Transfer as the sender with the given options.
        /// </summary>
        public abstract Task SendAsync(IMessageChannel channel, ObliviousTransferOptions options);

        /// <summary>
        /// Starts  K-fold 1-out-of-N Oblivious Transfer as the receiver with the given choice indices.
        /// </summar>
        public abstract Task<byte[][]> ReceiveAsync(IMessageChannel channel, int[] selectionIndices, int numberOfOptions, int numberOfMessageBits);
        
        /// <summary>
        /// Security level provided by the Oblious Transfer.
        ///
        /// The security level λ is the power-of-two exponent such that the expected runtime for an attacker
        /// to break the OT protocol with probability p is at least p * 2^λ in the semi-honest model.
        /// </summary>
        public abstract int SecurityLevel { get; }

        /// <summary>
        /// Starts a K-fold 1-out-of-2 Oblivious Transfer as the receiver with the given choice indices.
        /// </summary>
        public async Task<byte[][]> ReceiveAsync(IMessageChannel channel, BitArrayBase selectionIndices, int numberOfMessageBits)
        {
            int numberOfInvocations = selectionIndices.Length;
            int[] selectionIndicesAsInts = selectionIndices.Select(b => (int)b).ToArray();
            return await ReceiveAsync(channel, selectionIndicesAsInts, 2, numberOfMessageBits);
        }
    }
}
