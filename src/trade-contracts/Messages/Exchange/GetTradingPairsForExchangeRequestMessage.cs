namespace trade_contracts.Messages.Exchange
{
    public class GetTradingPairsForExchangeRequestMessage : RequestMessage
    {
        public string Exchange { get; set; }
        public CachePolicyContract CachePolicy { get; set; }
    }
}
