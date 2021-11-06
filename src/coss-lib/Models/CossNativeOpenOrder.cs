using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coss_lib.Models
{
    public class CossNativeOpenOrder
    {
        // "amount": "0.01000000",
        [JsonProperty("amount")]
        public decimal? Amount { get; set; }

        // "created_at": 1535822792981,
        [JsonProperty("created_at")]
        public decimal? Created_at { get; set; }

        // "order_guid": "af329d9d-cfb2-41ec-9404-b4ca654c5bf6",
        [JsonProperty("order_guid")]
        public string Order_guid { get; set; }

        // "pair_id": "eth-btc",
        [JsonProperty("pair_id")]
        public string PairId { get; set; }

        // "price": "0.03900000",
        [JsonProperty("price")]
        public decimal? Price { get; set; }

        // "total": "0.00039000",
        [JsonProperty("total")]
        public decimal? Total { get; set; }

        // "type": "buy",
        [JsonProperty("type")]
        public string Type { get; set; }

        // "tradeType": "limit-order"
        public string TradeType { get; set; }
    }
}
