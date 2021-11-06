namespace trade_contracts
{
    public class OrderContract
    {
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }

        public OrderContract() { }
        public OrderContract(decimal price, decimal quantity) { Price = price; Quantity = quantity; }
    }
}
