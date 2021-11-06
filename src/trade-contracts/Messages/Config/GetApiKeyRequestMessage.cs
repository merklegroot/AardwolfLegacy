namespace trade_contracts.Messages.Config
{
    public class GetApiKeyRequestMessage : RequestMessage
    {
        public string Exchange { get; set; }
    }
}
