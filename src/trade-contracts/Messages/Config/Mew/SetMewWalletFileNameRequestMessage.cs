namespace trade_contracts.Messages.Config.Mew
{
    public class SetMewWalletFileNameRequestMessage : RequestMessage
    {
        public RequestPayload Payload { get; set; }

        public class RequestPayload
        {
            public string FileName { get; set; }
        }
    }
}
