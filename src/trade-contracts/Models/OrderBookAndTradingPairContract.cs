
namespace trade_contracts
{
    public class OrderBookAndTradingPairContract : OrderBookContract
    {
        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }
    }
}
