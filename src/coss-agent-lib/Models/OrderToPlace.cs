using trade_model;

namespace trade_browser_lib.Models
{
    public class OrderToPlace
    {
        public OrderType OrderType { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
    }
}
