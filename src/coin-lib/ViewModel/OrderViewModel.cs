using trade_contracts;
using trade_model;

namespace coin_lib.ViewModel
{
    public class OrderViewModel
    {
        public decimal? Price { get; set; }
        public decimal? Quantity { get; set; }
        public bool IsGoodOrder { get; set; }

        public static OrderViewModel FromModel(Order model)
        {
            return model != null
            ? new OrderViewModel
            {
                Price = model.Price,
                Quantity = model.Quantity
            }
            : null;
        }

        public static OrderViewModel FromModel(OrderContract model)
        {
            return model != null
            ? new OrderViewModel
            {
                Price = model.Price,
                Quantity = model.Quantity
            }
            : null;
        }
    }
}
