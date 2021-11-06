namespace trade_contracts.Messages.Exchange
{
    public class GetBalanceRequestMessage : RequestMessage
    {
        public string Exchange { get; set; }
        public CachePolicyContract CachePolicy { get; set; }
    }
}
