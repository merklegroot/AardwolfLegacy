namespace trade_contracts.Messages.Exchange.PlaceOrder
{
    public class LimitResponseMessage : ResponseMessage
    {
        public ResponsePayload Payload { get; set; } = new ResponsePayload();

        public class ResponsePayload
        {
            public string OrderId { get; set; }
            public decimal? Quantity { get; set; }
            public decimal? Price { get; set; }
            public decimal? Executed { get; set; }
        }
    }
}
