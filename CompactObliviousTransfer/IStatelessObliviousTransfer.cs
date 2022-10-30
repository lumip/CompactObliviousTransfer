// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

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
    public interface IStatelessObliviousTransfer : ICostEstimator
    {
        /// <summary>
        /// Starts a K-fold 1-out-of-N Oblivious Transfer as the sender with the given options.
        /// </summary>
        Task SendAsync(IMessageChannel channel, ObliviousTransferOptions options);

        /// <summary>
        /// Starts  K-fold 1-out-of-N Oblivious Transfer as the receiver with the given choice indices.
        /// </summar>
        Task<ObliviousTransferResult> ReceiveAsync(IMessageChannel channel, int[] selectionIndices, int numberOfOptions, int numberOfMessageBits);
                
        /// <summary>
        /// Security level provided by the Oblious Transfer.
        ///
        /// The security level λ is the power-of-two exponent such that the expected runtime for an attacker
        /// to break the OT protocol with probability p is at least p * 2^λ in the semi-honest model.
        /// </summary>
        int SecurityLevel { get; }

    }
}
