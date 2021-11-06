namespace trade_contracts.Messages.Exchange
{
    public class GetDetailedCommodityForExchangeRequestMessage : RequestMessage
    {
        public string Exchange { get; set; }
        public string Symbol { get; set; }
        public string NativeSymbol { get; set; }
        public CachePolicyContract CachePolicy { get; set; }
    }
}
