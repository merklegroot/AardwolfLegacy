using Binance.Net.Objects;

namespace binance_lib.Models.Canonical
{
    public class BcOrderBookEntry
    {
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }

        public static BcOrderBookEntry FromModel(BinanceOrderBookEntry model)
        {
            return model != null
                ? new BcOrderBookEntry { Price = model.Price, Quantity = model.Quantity }
                : null;
        }
    }
}
