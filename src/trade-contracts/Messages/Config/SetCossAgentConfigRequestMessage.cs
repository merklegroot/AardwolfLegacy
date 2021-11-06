namespace trade_contracts.Messages.Config
{
    public class SetCossAgentConfigRequestMessage : RequestMessage
    {
        public CossAgentConfigContract Payload { get; set; }
    }
}
