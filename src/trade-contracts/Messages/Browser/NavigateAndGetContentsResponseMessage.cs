namespace trade_contracts.Messages.Browser
{
    public class NavigateAndGetContentsResponseMessage : ResponseMessage
    {
        public ResponsePayload Payload { get; set; }

        public class ResponsePayload
        {
            public string Contents { get; set; }
        }
    }
}
