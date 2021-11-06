using Newtonsoft.Json;

namespace qryptos_lib.Client
{
    public class QryptosPlaceOrderRequest
    {
        //   "order": {
        [JsonProperty("order")]
        public OrderPayload Order { get; set; }

        public class OrderPayload
        {
            // "order_type": "limit",
            [JsonProperty("order_type")]
            public string OrderType { get; set; }

            // "product_id": 1,
            [JsonProperty("product_id")]
            public long ProductId { get; set; }

            // "side": "sell",
            [JsonProperty("side")]
            public string Side { get; set; }

            // "quantity": "0.01",
            [JsonProperty("quantity")]
            public decimal Quantity { get; set; }

            // "price": "500.0"
            [JsonProperty("price")]
            public decimal Price { get; set; }
        }
    }
}
