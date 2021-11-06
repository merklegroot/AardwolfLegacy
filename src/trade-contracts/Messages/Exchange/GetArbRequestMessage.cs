namespace trade_contracts.Messages.Exchange
{
    public class GetArbRequestMessage : RequestMessage
    {
        public string ExchangeA { get; set; }
        public string ExchangeB { get; set; }
        public string Symbol { get; set; }
        public CachePolicyContract CachePolicy { get; set; }
    }
}
