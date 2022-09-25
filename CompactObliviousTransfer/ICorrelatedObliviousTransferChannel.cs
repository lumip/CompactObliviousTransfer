using System.Threading.Tasks;

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
    /// 
    public interface ICorrelatedObliviousTransferChannel : IObliviousTransferChannelReceiverEndpoint, ICostEstimator
    {
        Task<ObliviousTransferResult> SendAsync(ObliviousTransferOptions correlations);
    }
    
}
