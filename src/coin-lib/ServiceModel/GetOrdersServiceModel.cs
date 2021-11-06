namespace coin_lib.ServiceModel
{
    public class GetOrdersServiceModel
    {
        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }
        public string ExchangeA { get; set; }
        public string ExchangeB { get; set; }
        public string CachePolicy { get; set; }
    }
}
