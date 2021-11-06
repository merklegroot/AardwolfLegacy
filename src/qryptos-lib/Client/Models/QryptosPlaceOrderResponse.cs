using Newtonsoft.Json;

namespace qryptos_lib.Client
{
    public class QryptosPlaceOrderResponse
    {
        //"id": 2157474,
        [JsonProperty("id")]
        public string Id { get; set; }

        //"order_type": "limit",
        [JsonProperty("order_type")]
        public string OrderType { get; set; }

        //"quantity": "0.01",
        [JsonProperty("quantity")]
        public decimal? Quantity { get; set; }

        //"disc_quantity": "0.0",
        [JsonProperty("disc_quantity")]
        public decimal? DiscQuantity { get; set; }

        //"iceberg_total_quantity": "0.0",
        [JsonProperty("iceberg_total_quantity")]
        public decimal? IcebergTotalQuantity { get; set; }

        //"side": "sell",
        [JsonProperty("side")]
        public string Side { get; set; }

        //"filled_quantity": "0.0",
        [JsonProperty("filled_quantity")]
        public decimal FilledQuantity { get; set; }

        //"price": "500.0",
        [JsonProperty("price")]
        public decimal Price { get; set; }

        //"created_at": 1462123639,
        [JsonProperty("created_at")]
        public long? CreatedAt { get; set; }

        //"updated_at": 1462123639,
        [JsonProperty("updated_at")]
        public long? UpdatedAt { get; set; }

        //"status": "live",
        [JsonProperty("status")]
        public string Status { get; set; }

        //"leverage_level": 1,
        [JsonProperty("leverage_level")]
        public int? LeverageLevel { get; set; }

        //"source_exchange": "QUOINE",
        [JsonProperty("source_exchange")]
        public string SourceExchange { get; set; }

        //"product_id": 1,
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        //"product_code": "CASH",
        [JsonProperty("product_code")]
        public string ProductCode { get; set; }

        //"funding_currency": "USD",
        [JsonProperty("funding_currency")]
        public string FundingCurrency { get; set; }

        //"currency_pair_code": "BTCUSD",
        [JsonProperty("currency_pair_code")]
        public string CurrencyPairCode { get; set; }

        //"order_fee": "0.0"
        [JsonProperty("order_fee")]
        public decimal? OrderFee { get; set; }
    }
}
