namespace trade_contracts.Messages.Exchange
{
    public class GetExchangesForCommodityRequestMessage : RequestMessage
    {
        public string Symbol { get; set; }
        public CachePolicyContract CachePolicy { get; set; }
    }
}
