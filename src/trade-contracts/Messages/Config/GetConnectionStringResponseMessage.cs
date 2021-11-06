namespace trade_contracts.Messages.Config
{
    public class GetConnectionStringResponseMessage : ResponseMessage
    {
        public string ConnectionString { get; set; }
    }
}
