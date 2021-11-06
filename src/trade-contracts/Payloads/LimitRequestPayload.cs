namespace trade_contracts.Payloads
{
    public class LimitRequestPayload
    {
        public string Exchange { get; set; }
        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
