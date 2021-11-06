namespace trade_contracts.Messages.Config
{
    public class SetApiKeyRequestMessage : RequestMessage
    {
        public RequestPayload Payload { get; set; }

        public class RequestPayload
        {
            public string Exchange { get; set; }
            public string Key { get; set; }
            public string Secret { get; set; }
        }
    }
}
