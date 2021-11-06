namespace trade_contracts.Messages.Exchange
{
    public class GetAggregateExchangeHistoryRequestMessage : RequestMessage
    {
        public RequestPayload Payload { get; set; }

        public class RequestPayload
        {
            public int? Limit { get; set; }
            public CachePolicyContract CachePolicy { get; set; }
        }
    }
}
