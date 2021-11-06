namespace trade_model
{
    public class LimitOrderResult
    {
        public bool WasSuccessful { get; set; }
        public string FailureReason { get; set; }
        public string OrderId { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Price { get; set; }
        public decimal? Executed { get; set; }
    }
}
