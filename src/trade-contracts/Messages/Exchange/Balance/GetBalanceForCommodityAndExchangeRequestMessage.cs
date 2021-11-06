namespace trade_contracts.Messages.Exchange
{
    public class GetBalanceForCommodityAndExchangeRequestMessage : RequestMessage
    {
        public string Exchange { get; set; }
        public string Symbol { get; set; }
        public CachePolicyContract CachePolicy { get; set; }
    }
}
