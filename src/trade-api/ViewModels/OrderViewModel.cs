using coss_model;
using trade_model;

namespace trade_api.ViewModels
{
    public class OrderViewModel
    {
        public decimal? Price { get; set; }
        public decimal? Quantity { get; set; }
        public bool IsGoodOrder { get; set; }

        public static OrderViewModel FromModel(CossIntegratedMarketInfo.Order model)
        {
            return model != null
            ? new OrderViewModel
            {
                Price = model.Price,
                Quantity = model.Quantity
            }
            : null;
        }

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
    }
}