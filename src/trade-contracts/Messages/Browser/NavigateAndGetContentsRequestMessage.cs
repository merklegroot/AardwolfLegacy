namespace trade_contracts.Messages.Browser
{
    public class NavigateAndGetContentsRequestMessage : RequestMessage
    {
        public RequestPayload Payload { get; set; }

        public class RequestPayload
        {
            public string Url { get; set; }
        }
    }
}
