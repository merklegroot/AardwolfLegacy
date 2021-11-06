using Newtonsoft.Json;
using System.Collections.Generic;

namespace oex_lib.Models
{
    public class OexGetOrderBookResponse
    {
        // "code": 200,
        [JsonProperty("code")]
        public int Code { get; set; }

        // "msg": "SUCCESS",
        [JsonProperty("msg")]
        public string Msg { get; set; }

        // "time": 1540748973054,
        [JsonProperty("time")]
        public long Time { get; set; }

        [JsonProperty("data")]
        public ResponseData Data { get; set; }

        public class ResponseData
        {
            // This field is usually an empty string.
            // Leaving it off for now to prevent confusion.
            // "symbol": "",
            //[JsonProperty("symbol")]
            //public string Symbol { get; set; }

            [JsonProperty("buys")]
            public List<OexOrder> Buys { get; set; }

            [JsonProperty("p_open")]
            public decimal? POpen { get; set; }

            [JsonProperty("sells")]
            public List<OexOrder> Sells { get; set; }

            [JsonProperty("p_new")]
            public decimal? PNew { get; set; }

            [JsonProperty("trades")]
            public List<OexTrade> Trades { get; set; }
        }
    }
}
