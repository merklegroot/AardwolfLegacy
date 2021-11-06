namespace trade_contracts.Messages.Config.Mew
{
    public class GetMewWalletFileNameResponseMessage : ResponseMessage
    {
        public ResponsePayload Payload { get; set; }

        public class ResponsePayload
        {
            public string FileName { get; set; }
        }
    }
}
