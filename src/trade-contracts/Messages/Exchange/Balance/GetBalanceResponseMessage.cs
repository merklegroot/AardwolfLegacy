namespace trade_contracts.Messages.Exchange
{
    public class GetBalanceResponseMessage : ResponseMessage
    {
        public BalanceInfoContract BalanceInfo { get; set; }
    }
}
