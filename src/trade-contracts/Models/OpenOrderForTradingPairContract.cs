namespace trade_contracts
{
    public class OpenOrderForTradingPairContract
    {
        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }

        public string OrderId { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public OrderTypeContractEnum OrderType { get; set; }
    }
}
