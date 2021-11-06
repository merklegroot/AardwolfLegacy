namespace trade_contracts.Messages.Exchange
{
    public class GetOrderBookRequestMessage : RequestMessage
    {
        public string Exchange { get; set; }
        public TradingPairContract TradingPair { get; set; }
        public CachePolicyContract CachePolicy { get; set; }
    }
}
