namespace trade_contracts.Messages.Config
{
    public class GetBitzAgentConfigResponseMessage : ResponseMessage
    {
        public AgentConfigContract BitzAgentConfig { get; set; }
    }
}
