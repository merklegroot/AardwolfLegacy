
namespace livecoin_lib.Models
{
    public class LivecoinCancelOrderResponse
    {
        //"success": true,
        public bool Success { get; set; }

        //"cancelled": true,
        public bool Cancelled { get; set; }

        //"exception": null,

        //"quantity": 8.01700502,
        public decimal? Quantity { get; set; }

        //"tradeQuantity": 0E-8,
        public decimal? TradeQuantity { get; set; }

        //"message": null
    }
}
