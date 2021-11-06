namespace trade_contracts.Messages.Exchange
{
    public class GetCommodityDetailsRequestMessage : RequestMessage
    {
        public string Symbol { get; set; }
        public CachePolicyContract CachePolicy { get; set; }
    }
}
