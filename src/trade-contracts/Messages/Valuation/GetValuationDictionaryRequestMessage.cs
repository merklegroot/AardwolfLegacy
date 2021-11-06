namespace trade_contracts.Messages.Valuation
{
    public class GetValuationDictionaryRequestMessage : RequestMessage
    {
        public RequestPayload Payload { get; set; }

        public class RequestPayload
        {
            public CachePolicyContract CachePolicy { get; set; }
        }
    }
}
