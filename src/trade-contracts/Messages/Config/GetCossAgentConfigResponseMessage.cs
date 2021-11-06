namespace trade_contracts.Messages.Config
{
    public class GetCossAgentConfigResponseMessage : ResponseMessage
    {
        public CossAgentConfigContract Payload { get; set; }
    }
}
