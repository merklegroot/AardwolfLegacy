namespace trade_contracts.Messages.Config
{
    public class SetCcxtUrlRequestMessage : RequestMessage
    {
        public RequestPayload Payload { get; set; }

        public class RequestPayload
        {
            public string Url { get; set; }
        }
    }
}
