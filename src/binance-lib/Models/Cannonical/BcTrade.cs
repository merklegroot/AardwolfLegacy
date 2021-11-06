using Binance.Net.Objects;
using System;

namespace binance_lib.Models.Canonical
{
    public class BcTrade
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal Commission { get; set; }
        public string CommissionAsset { get; set; }
        public DateTime Time { get; set; }
        public bool IsBuyer { get; set; }
        public bool IsMaker { get; set; }
        public bool IsBestMatch { get; set; }

        public static BcTrade FromModel(BinanceTrade model)
        {
            if (model == null) { return null; }

            return new BcTrade
            {
                Id = model.Id,
                OrderId = model.OrderId,
                Price = model.Price,
                Quantity = model.Quantity,
                Commission = model.Commission,
                CommissionAsset = model.CommissionAsset,
                Time = model.Time,
                IsBuyer = model.IsBuyer,
                IsMaker = model.IsMaker,
                IsBestMatch = model.IsBestMatch
            };
        }
    }
}
