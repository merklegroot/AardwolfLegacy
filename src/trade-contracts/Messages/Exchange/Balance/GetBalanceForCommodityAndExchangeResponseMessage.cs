namespace trade_contracts.Messages.Exchange
{
    public class GetBalanceForCommodityAndExchangeResponseMessage : ResponseMessage
    {
        public BalanceContract Balance { get; set; }
    }
}
