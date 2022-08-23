namespace CompactOT
{

    public interface IBaseProtocolFactory
    {
        IObliviousTransferChannel MakeChannel(
            IMessageChannel channel, CryptoContext cryptoContext, int securityLevel
        );
    }
}
