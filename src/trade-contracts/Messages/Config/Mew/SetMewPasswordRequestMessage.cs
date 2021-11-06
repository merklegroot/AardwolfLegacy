namespace trade_contracts.Messages.Config
{
    public class SetMewPasswordRequestMessage : RequestMessage
    {
        public RequestPayload Payload { get; set; }

        public class RequestPayload
        {
            public string Password { get; set; }
        }
    }
}
