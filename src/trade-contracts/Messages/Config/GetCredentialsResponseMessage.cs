namespace trade_contracts.Messages.Config
{
    public class GetCredentialsResponseMessage : ResponseMessage
    {
        public UsernameAndPasswordContract Credentials { get; set; }
    }
}
