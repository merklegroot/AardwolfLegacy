namespace trade_contracts.Messages.Exchange
{
    public class GetDepositAddressResponseMessage : ResponseMessage
    {
        public DepositAddressContract DepositAddress { get; set; }
    }
}
