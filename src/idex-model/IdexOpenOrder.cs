using System;

namespace idex_model
{
    public class IdexOpenOrder
    {
        public string Market { get; set; }
        public string TradeType { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal Total { get; set; }
        public DateTime DateTimeUtc { get; set; }
    }
}
