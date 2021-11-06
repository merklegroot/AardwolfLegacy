using trade_model;

namespace coss_data_model
{
    public class CossOpenOrder
    {
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public OrderType OrderType { get; set; }
        public string OrderTypeText { get { return OrderType.ToString(); } set { } }
    }
}
