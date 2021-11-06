namespace trade_contracts.Messages.Exchange
{
    public class GetOpenOrdersForTradingPairRequestMessageV2 : RequestMessage
    {
        public string Exchange { get; set; }
        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }
        public CachePolicyContract CachePolicy { get; set; }
    }
}
