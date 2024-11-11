// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Threading;
using System.Threading.Tasks;

namespace CompactOT
{

    /// <summary>
    /// A 1-out-of-N Oblivious Transfer receiver endpoint.
    ///
    /// Provides a 1ooN-OT client/receiver view for a given channel
    /// (i.e., pair of parties) and may maintain channel-specific protocol
    /// state in-between invocations.
    ///
    /// In a single invocation of 1-out-of-N Oblivious Transfer, the sender inputs
    /// N bit-strings x_0, ..., x_N and the receiver inputs a selection index s.
    /// The sender receives no outputs. The receiver receives as output the bit-string x_s.
    /// </summary>
    public interface IObliviousTransferChannelReceiverEndpoint
    {
        Task<ObliviousTransferResult> ReceiveAsync(
            int[] selectionIndices,
            int numberOfOptions,
            int numberOfMessageBits,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// The network channel the OT operates on, uniquely identifying the pair of parties involved in the OT.
        /// </summary>
        IMessageChannel Channel { get; }

        int SecurityLevel { get; }
    }

}
