using System.Threading.Tasks;

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
    public interface IRandomObliviousTransferChannel : IObliviousTransferChannelReceiverEndpoint, ICostEstimator
    {
        Task<ObliviousTransferOptions> SendAsync(int numberOfInvocations, int numberOfOptions, int numberOfMessageBits);
    }
    
}
