namespace trade_model
{
    public class OpenOrder
    {
        public string OrderId { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public OrderType OrderType { get; set; }
        public string OrderTypeText { get { return OrderType.ToString(); } set { } }  
    }
}
