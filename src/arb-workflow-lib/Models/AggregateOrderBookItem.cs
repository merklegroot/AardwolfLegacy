using trade_model;

namespace arb_workflow_lib.Models
{
    public class AggregateOrderBookItem
    {
        public OrderType OrderType { get; set; }
        public string BaseSymbol { get; set; }
        public decimal NativePrice { get; set; }
        public decimal UsdPrice { get; set; }
        public decimal Quantity { get; set; }
    }
}
