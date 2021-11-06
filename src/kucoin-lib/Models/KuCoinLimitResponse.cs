using Newtonsoft.Json;
using System;

namespace kucoin_lib.Models
{
    public class KuCoinLimitResponse
    {
        [JsonProperty("info")]
        public KuCoinLimitResponseInfo Info { get; set; }

        public class KuCoinLimitResponseInfo
        {
            // "success": true,
            [JsonProperty("success")]
            public bool Success { get; set; }

            // "code": "OK",
            [JsonProperty("code")]
            public string Code { get; set; }

            // "msg": "OK",
            [JsonProperty("msg")]
            public string Message { get; set; }

            // "timestamp": 1537926207628,
            [JsonProperty("timestamp")]
            public long timestamp { get; set; }

            // "data": {
            [JsonProperty("data")]
            public InfoData Data { get; set; }

            public class InfoData
            {
                // "orderOid": "5baae43f54c4fa614716334b"
                [JsonProperty("orderOid")]
                public string OrderOid { get; set; }
            }

        }

        // "id": "5baae43f54c4fa614716334b",
        [JsonProperty("id")]
        public string Id { get; set; }

        // "timestamp": 1537926207628,
        [JsonProperty("timestamp")]
        public long TimeStamp { get; set; }

        // "datetime": "2018-09-26T01:43:27.628Z",
        [JsonProperty("datetime")]
        public DateTime? DateTime { get; set; }

        // "symbol": "LA/ETH",
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        // "type": "limit",
        [JsonProperty("type")]
        public string Type { get; set; }

        // "side": "buy",
        [JsonProperty("side")]
        public string Side { get; set; }

        // "amount": 1848.0398,
        [JsonProperty("amount")]
        public decimal? Amount { get; set; }

        // "price": 0.0002979,
        [JsonProperty("price")]
        public decimal? Price { get; set; }

        // "cost": 0.55053105642,
        [JsonProperty("cost")]
        public decimal? Cost { get; set; }

        // "status": "open"
        [JsonProperty("status")]
        public string Status { get; set; }
    }
}
