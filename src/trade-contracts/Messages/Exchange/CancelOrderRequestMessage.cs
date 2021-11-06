namespace trade_contracts.Messages
{
    public class CancelOrderRequestMessage : RequestMessage
    {
        public string Exchange { get; set; }
        public string OrderId { get; set; }
        //public string Symbol { get; set; }
        //public string BaseSymbol { get; set; }
        //public decimal Price { get; set; }
        //public decimal Quantity { get; set; }
    }
}
