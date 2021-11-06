namespace trade_contracts.Messages
{
    public class ResponseMessage : MessageBase, IResponseMessage
    {
        public bool WasSuccessful { get; set; }
        public string FailureReason { get; set; }
    }
}
