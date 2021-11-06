using Newtonsoft.Json;

namespace coss_api_client_lib.Models
{
    public class CreateApiOrderResponseMessage
    {
        // "account_id": "95e0fffe-6014-4414-8c51-d8a96e12b980",
        [JsonProperty("account_id")]
        public string AccountId { get; set; }

        // "order_side": "BUY",
        [JsonProperty("order_side")]
        public string OrderSide { get; set; }

        // "status": "open",
        [JsonProperty("status")]
        public string Status { get; set; }

        // "order_id": "8a153b39-17ab-4424-971a-f56ba6d720f8",
        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        // "order_size": "0.01",
        [JsonProperty("order_size")]
        public decimal OrderSize { get; set; }

        // "order_price": "0.025",
        [JsonProperty("order_price")]
        public decimal OrderPrice { get; set; }

        // "total": "0.00025 btc",
        [JsonProperty("total")]
        public string Total { get; set; }

        // "createTime": 1542118133036,
        [JsonProperty("createTime")]
        public long CreateTime { get; set; }

        // "order_symbol": "eth-btc",
        [JsonProperty("order_symbol")]
        public string OrderSymbol { get; set; }

        // "avg": "0.00000000",
        [JsonProperty("avg")]
        public decimal Avg { get; set; }

        // "executed": "0",
        [JsonProperty("executed")]
        public decimal Executed { get; set; }

        // "stop_price": "0.00000000",
        [JsonProperty("stop_price")]
        public decimal StopPrice { get; set; }

        // "type": "limit"
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
