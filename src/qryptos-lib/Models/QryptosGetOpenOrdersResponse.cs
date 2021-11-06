using Newtonsoft.Json;
using System.Collections.Generic;

namespace qryptos_lib.Client
{
    public class QryptosGetOpenOrdersResponse
    {
        [JsonProperty("models")]
        public List<ResponseModel> Models { get; set; }

        // "total_pages":1,
        [JsonProperty("total_pages")]
        public int? TotalPages { get; set; }

        // "current_page":1}
        [JsonProperty("current_page")]
        public int? CurrentPage { get; set; }

        public class ResponseModel
        {
            // "id": 504290310,
            [JsonProperty("id")]
            public string Id { get; set; }

            // "order_type": "limit",
            [JsonProperty("order_type")]
            public string OrderType { get; set; }

            // "quantity": "15696.67099923",
            [JsonProperty("quantity")]
            public decimal? Quantity { get; set; }

            // "disc_quantity": "0.0",
            [JsonProperty("disc_quantity")]
            public decimal? DiscQuantity { get; set; }

            // "iceberg_total_quantity": "0.0",
            [JsonProperty("iceberg_total_quantity")]
            public decimal? IcebergTotalQuantity { get; set; }

            // "side": "buy",
            [JsonProperty("side")]
            public string Side { get; set; }

            // "filled_quantity": "0.0",
            [JsonProperty("filled_quantity")]
            public decimal? FilledQuantity { get; set; }

            // "price": 0.00615061,
            [JsonProperty("price")]
            public decimal? Price { get; set; }

            // "created_at": 1539352581,
            [JsonProperty("created_at")]
            public long? created_at { get; set; }

            // "updated_at": 1539352581,
            [JsonProperty("updated_at")]
            public long? updated_at { get; set; }

            // "status": "live",
            [JsonProperty("status")]
            public string Status { get; set; }

            // "leverage_level": 1,
            [JsonProperty("leverage_level")]
            public int? LeverageLevel { get; set; }

            // "source_exchange": null,
            [JsonProperty("source_exchange")]
            public string SourceExchange { get; set; }

            // "product_id": 150,
            [JsonProperty("product_id")]
            public string ProductId { get; set; }

            // "product_code": "CASH",
            [JsonProperty("product_code")]
            public string ProductCode { get; set; }

            // "funding_currency": "QASH",
            [JsonProperty("funding_currency")]
            public string FundingCurrency { get; set; }

            // "crypto_account_id": null,
            [JsonProperty("crypto_account_id")]
            public string CryptoAccountId { get; set; }

            // "currency_pair_code": "DENTQASH",
            [JsonProperty("currency_pair_code")]
            public string CurrencyPairCode { get; set; }

            // "average_price": "0.0",
            [JsonProperty("average_price")]
            public decimal? AveragePrice { get; set; }

            // "target": "spot",
            [JsonProperty("target")]
            public string Target { get; set; }

            // "order_fee": "0.0",
            [JsonProperty("order_fee")]
            public decimal? OrderFee { get; set; }

            // "source_action": "manual",
            [JsonProperty("source_action")]
            public string SourceAction { get; set; }

            // "unwound_trade_id": null,
            [JsonProperty("unwound_trade_id")]
            public string UnwoundTradeId { get; set; }

            // "trade_id": null
            [JsonProperty("trade_id")]
            public string TradeId { get; set; }
        }
    }
}
