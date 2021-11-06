namespace trade_contracts.Messages.Config
{
    public class SetPasswordRequestMessage : RequestMessage
    {
        public RequestPayload Payload { get; set; }

        public class RequestPayload
        {
            public string Key { get; set; }
            public string Password { get; set; }
        }
    }
}
