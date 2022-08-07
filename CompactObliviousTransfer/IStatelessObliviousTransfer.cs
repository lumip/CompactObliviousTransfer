using System.Threading.Tasks;

namespace CompactOT
{
    /// <summary>
    /// A stateless 1-out-of-N Oblivious Transfer implementation.
    /// 
    /// Stateless here means that the OT implementation does not maintain state for each channel (i.e., pair of communicating parties)
    /// in-between invocations.
    /// 
    /// In a single invocation of 1-out-of-N Oblivious Transfer, the sender inputs
    /// N bit-strings x_0, ..., x_N and the receiver inputs a selection index s.
    /// The sender receives no outputs. The receiver receives as output the bit-string x_s.
    /// </summary>
    public interface IStatelessObliviousTransfer
    {
        /// <summary>
        /// Starts a K-fold 1-out-of-N Oblivious Transfer as the sender with the given options.
        /// </summary>
        public Task SendAsync(IMessageChannel channel, ObliviousTransferOptions options);

        /// <summary>
        /// Starts  K-fold 1-out-of-N Oblivious Transfer as the receiver with the given choice indices.
        /// </summar>
        public Task<ObliviousTransferResult> ReceiveAsync(IMessageChannel channel, int[] selectionIndices, int numberOfOptions, int numberOfMessageBits);
                
        /// <summary>
        /// Security level provided by the Oblious Transfer.
        ///
        /// The security level λ is the power-of-two exponent such that the expected runtime for an attacker
        /// to break the OT protocol with probability p is at least p * 2^λ in the semi-honest model.
        /// </summary>
        public int SecurityLevel { get; }

        public double EstimateCost(ObliviousTransferUsageProjection usageProjection);

    }
}
