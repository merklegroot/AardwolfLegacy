namespace trade_contracts.Messages.Exchange.PlaceOrder
{
    public abstract class LimitRequestMessage : RequestMessage
    {
        public string Exchange { get; set; }
        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
