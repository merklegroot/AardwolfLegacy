namespace trade_contracts.Messages.Exchange
{
    public class GetWithdrawalFeeRequestMessage : RequestMessage
    {
        public string Exchange { get; set; }
        public string Symbol { get; set; }
        public CachePolicyContract CachePolicy { get; set; }
    }
}
