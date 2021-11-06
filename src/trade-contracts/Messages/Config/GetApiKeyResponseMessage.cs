namespace trade_contracts.Messages.Config
{
    public class GetApiKeyResponseMessage : ResponseMessage
    {
        public string Exchange { get; set; }
        public ApiKeyContract ApiKey { get; set; }
    }
}
