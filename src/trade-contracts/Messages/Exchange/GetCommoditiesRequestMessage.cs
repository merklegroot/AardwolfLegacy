namespace trade_contracts.Messages.Exchange
{
    public class GetCommoditiesRequestMessage : RequestMessage
    {
        public CachePolicyContract CachePolicy { get; set; }
    }
}
