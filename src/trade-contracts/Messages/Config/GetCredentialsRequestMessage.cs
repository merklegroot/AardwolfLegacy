namespace trade_contracts.Messages.Config
{
    public class GetCredentialsRequestMessage : RequestMessage
    {
        public string Exchange { get; set; }
    }
}
