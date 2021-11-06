using Newtonsoft.Json;

namespace idex_integration_lib.Models
{
    public class IdexTradeHistoryItem
    {
        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("amount")]
        public decimal? Amount { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("total")]
        public decimal? Total { get; set; }

        [JsonProperty("price")]
        public decimal? Price { get; set; }

        [JsonProperty("orderHash")]
        public string OrderHash { get; set; }

        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("buyerFee")]
        public decimal? BuyerFee { get; set; }

        [JsonProperty("sellerFee")]
        public decimal? SellerFee { get; set; }

        [JsonProperty("timestamp")]
        public double? Timestamp { get; set; }

        [JsonProperty("maker")]
        public string Maker { get; set; }

        [JsonProperty("taker")]
        public string Taker { get; set; }

        [JsonProperty("transactionHash")]
        public string TransactionHash { get; set; }

        [JsonProperty("usdValue")]
        public decimal? UsdValue { get; set; }
    }
}
