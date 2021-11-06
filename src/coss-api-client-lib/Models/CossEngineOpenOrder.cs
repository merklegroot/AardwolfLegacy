using Newtonsoft.Json;

namespace coss_api_client_lib.Models
{
    public class CossEngineOpenOrder
    {
        // "hex_id": null,

        // "order_id": "8b566ff4-529c-42e3-8be8-fae63f73538a",
        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        // "account_id": "95e0fffe-6014-4414-8c51-d8a96e12b980",
        [JsonProperty("account_id")]
        public string AccountId { get; set; }

        // "order_symbol": "ETH_BTC",
        [JsonProperty("order_symbol")]
        public string OrderSymbol { get; set; }

        // "order_side": "BUY",
        [JsonProperty("order_side")]
        public string OrderSide { get; set; }

        // "status": "open",
        [JsonProperty("status")]
        public string Status { get; set; }

        // "createTime": 1544581231459,
        [JsonProperty("createTime")]
        public long CreateTime { get; set; }

        // "type": "limit",
        [JsonProperty("type")]
        public string Type { get; set; }

        // "timeMatching": 0,
        [JsonProperty("timeMatching")]
        public decimal TimeMatching { get; set; }

        // "order_price": "0.01245000",
        [JsonProperty("order_price")]
        public decimal OrderPrice { get; set; }

        // "order_size": "0.1",
        [JsonProperty("order_size")]
        public decimal OrderSize { get; set; }

        // "executed": "0",
        [JsonProperty("executed")]
        public decimal Executed { get; set; }

        // "stop_price": "0.00000000",
        [JsonProperty("stop_price")]
        public decimal StopPrice { get; set; }

        // "avg": "0.01245000",
        [JsonProperty("avg")]
        public decimal Avg { get; set; }

        // "total": "0.00124500 BTC"
        [JsonProperty("total")]
        public string Total { get; set; }
    }
}
