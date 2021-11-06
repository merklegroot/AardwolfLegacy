namespace trade_contracts.Messages.Exchange.History
{
    public class GetHistoryForTradingPairRequestMessage : RequestMessage
    {
        public class RequestPayload
        {
            public string Exchange { get; set; }
            public string Symbol { get; set; }
            public string BaseSymbol { get; set; }
            public CachePolicyContract CachePolicy { get; set; }
        }

        public RequestPayload Payload { get; set; }
    }
}
