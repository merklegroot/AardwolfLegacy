namespace trade_contracts.Messages.Config
{
    public class GetMewPasswordResponseMessage : ResponseMessage
    {
        public ResponsePayload Payload { get; set; }

        public class ResponsePayload
        {
            public string Password { get; set; }
        }
    }
}
