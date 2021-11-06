namespace trade_contracts.Messages.Exchange
{
    public class GetDetailedCommodityForExchangeResponseMessage : ResponseMessage
    {
        public DetailedExchangeCommodityContract Commodity { get; set; }
    }
}
