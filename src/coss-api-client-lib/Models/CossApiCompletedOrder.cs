using Newtonsoft.Json;

namespace coss_api_client_lib.Models
{
    public class CossApiCompletedOrder
    {
        // hex_id was showing up with values this morning.
        // now it's always null.
        // maybe it's an interal value that's going away?
        //[JsonProperty("hex_id")]
        //public string HexId { get; set; }

        //"order_id": guid,
        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        //"account_id": guid,
        [JsonProperty("account_id")]
        public string AcountId { get; set; }

        //"order_symbol": "COSS_ETH",
        [JsonProperty("order_symbol")]
        public string OrderSymbol { get; set; }

        //"order_side": "SELL",
        [JsonProperty("order_side")]
        public string OrderSide { get; set; }

        //"status": "partial_fill",
        [JsonProperty("status")]
        public string Status { get; set; }

        //"createTime": 1546165690479,
        [JsonProperty("createTime")]
        public long CreateTime { get; set; }

        //"type": "limit",
        [JsonProperty("type")]
        public string Type { get; set; }

        //"timeMatching": 0,
        [JsonProperty("timeMatching")]
        public long TimeMatching { get; set; }

        //"order_price": "0.00044300",
        [JsonProperty("order_price")]
        public decimal? OrderPrice { get; set; }

        //"order_size": "250",
        [JsonProperty("order_size")]
        public decimal? OrderSize { get; set; }

        //"executed": "138",
        [JsonProperty("executed")]
        public decimal? Executed { get; set; }

        //"stop_price": "0.00000000",
        [JsonProperty("stop_price")]
        public decimal? StopPrice { get; set; }

        //"avg": "0.00044300",
        [JsonProperty("avg")]
        public decimal? Avg { get; set; }

        //"total": "0.06113400 ETH"
        [JsonProperty("total")]
        public string Total { get; set; }
    }
}
