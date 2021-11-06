namespace trade_contracts.Models.OpenOrders
{
    public class OpenOrderContract
    {
        public string OrderId { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public OrderTypeContractEnum OrderType { get; set; }
    }
}
