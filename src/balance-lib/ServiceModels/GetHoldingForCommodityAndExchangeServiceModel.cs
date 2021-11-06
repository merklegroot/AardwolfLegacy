namespace balance_lib
{
    public class GetHoldingForCommodityAndExchangeServiceModel
    {
        public string Symbol { get; set; }
        public string Exchange { get; set; }
        public bool ForceRefresh { get; set; }
    }
}
