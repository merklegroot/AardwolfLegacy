namespace trade_contracts.Messages.Config
{
    public class GetPasswordRequestMessage : RequestMessage
    {
        public RequestPayload Payload { get; set; }

        public class RequestPayload
        {
            public string Key { get; set; }
        }
    }
}
