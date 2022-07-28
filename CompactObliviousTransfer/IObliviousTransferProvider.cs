namespace CompactOT
{
    /// <summary>
    /// Provides stateful Oblivious Transfer channel instances of the specific type indicated by the template argument
    /// for pairs of parties identified by network channels.
    /// </summary>
    public interface IObliviousTransferProvider
    {
        /// <summary>
        /// Creates a stateful Oblivious Transfer channel instance for the given network channel.
        /// </summary>
        /// <param name="channel">The network channel the OT operates on, uniquely identifying the pair of parties involved in the OT.</param>
        /// <returns>The stateful Random Oblivious Transfer channel.</returns>
        IObliviousTransferChannel CreateChannel(IMessageChannel channel);
    }
}
