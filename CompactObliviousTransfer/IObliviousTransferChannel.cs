// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Threading.Tasks;

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
    public interface IObliviousTransferChannel : IObliviousTransferChannelReceiverEndpoint, ICostEstimator
    {
        Task SendAsync(ObliviousTransferOptions options);
    }

}
