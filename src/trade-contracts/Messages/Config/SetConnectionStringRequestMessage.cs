namespace trade_contracts.Messages.Config
{
    public class SetConnectionStringRequestMessage : RequestMessage
    {
        public string ConnectionString { get; set; }
    }
}
