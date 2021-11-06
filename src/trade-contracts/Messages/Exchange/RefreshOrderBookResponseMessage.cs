namespace trade_contracts.Messages.Exchange
{
    public class RefreshOrderBookResponseMessage : ResponseMessage
    {
        public RefreshOrderBookResultContract Result { get; set; }
    }
}
