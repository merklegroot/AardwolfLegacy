namespace trade_contracts.Messages.Exchange
{
    public class GetCommoditiesForExchangeRequestMessage : RequestMessage
    {
        public string Exchange { get; set; }
        public CachePolicyContract CachePolicy { get; set; }
    }
}
