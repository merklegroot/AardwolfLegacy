namespace trade_contracts.Messages.Exchange
{
    public class GetCommodityDetailsResponseMessage : ResponseMessage
    {
        public CommodityDetailsContract Payload { get; set; }
    }
}
