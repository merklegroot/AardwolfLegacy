namespace trade_contracts.Messages.Exchange
{
    public class GetHitBtcHealthStatusRequestMessage : RequestMessage
    {
        public CachePolicyContract CachePolicy { get; set; }
    }
}
