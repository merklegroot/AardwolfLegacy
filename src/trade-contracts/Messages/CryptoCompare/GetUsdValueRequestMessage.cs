namespace trade_contracts.Messages.CryptoCompare
{
    public class GetUsdValueRequestMessage : RequestMessage
    {
        public string Symbol { get; set; }
        public CachePolicyContract CachePolicy { get; set; }
    }
}
