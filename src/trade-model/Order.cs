namespace trade_model
{
    public class Order
    {
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }

        public Order() { }
        public Order(decimal price, decimal quantity) { Price = price; Quantity = quantity; }

        public override string ToString()
        {
            return $"Price: {Price}, Quantity: {Quantity}";
        }
    }
}
