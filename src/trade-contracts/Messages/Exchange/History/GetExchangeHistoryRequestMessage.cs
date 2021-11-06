namespace trade_contracts.Messages.Exchange
{
    public class GetExchangeHistoryRequestMessage : RequestMessage
    {
        public string Exchange { get; set; }
        public int Limit { get; set; }
        public CachePolicyContract CachePolicy { get; set; }
    }
}
