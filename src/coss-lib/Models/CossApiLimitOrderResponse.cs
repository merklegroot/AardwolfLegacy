using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coss_lib.Models
{
    public class CossApiLimitOrderResponse
    {
        [JsonProperty("account_id")]
        public string AccountId { get; set; }

        [JsonProperty("order_side")]
        //"order_side": "SELL",
        public string OrderSide { get; set; }

        //"status": "open",
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        //"order_size": "0.01",
        [JsonProperty("order_size")]
        public decimal OrderSize { get; set; }

        //"order_price": "0.035",
        [JsonProperty("order_price")]
        public decimal OrderPrice { get; set; }

        //"total": "0.00035 btc",
        [JsonProperty("total")]
        public string Total { get; set; }

        //"createTime": 1542991251147,
        [JsonProperty("createTime")]
        public long CreateTime { get; set; }

        //"order_symbol": "eth-btc",
        [JsonProperty("order_symbol")]
        public string OrderSymbol { get; set; }

        //"avg": "0.00000000",
        [JsonProperty("avg")]
        public decimal Avg { get; set; }

        //"executed": "0",
        [JsonProperty("executed")]
        public decimal Executed { get; set; }

        //"stop_price": "0.00000000",
        [JsonProperty("stop_price")]
        public decimal StopPrice { get; set; }

        //"type": "limit"
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
