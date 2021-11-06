using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kucoin_lib.Models
{
    public class KucoinCcxtBuyLimitResponse
    {
        //	"info": {
        [JsonProperty("info")]
        public ResponseInfo Info { get; set; }

        public class ResponseInfo
        {
            //		"success": true,
            [JsonProperty("success")]
            public bool Success { get; set; }

            //		"code": "OK",
            [JsonProperty("code")]
            public string Code { get; set; }

            //		"msg": "OK",
            [JsonProperty("msg")]
            public string Msg { get; set; }

            //		"timestamp": 1531579286542,
            [JsonProperty("timestamp")]
            public string TimeStamp { get; set; }

            //		"data": {
            //			"orderOid": "5b4a0b96d06990232e188a0e"
            //		}
        }

        //	"id": "5b4a0b96d06990232e188a0e",
        [JsonProperty("id")]
        public string Id { get; set; }

        //	"timestamp": 1531579286542,
        [JsonProperty("timestamp")]
        public long TimeStamp { get; set; }

        //	"datetime": "2018-07-14T14:41:26.542Z",
        [JsonProperty("datetime")]
        public string DateTime { get; set; }

        //	"symbol": "HAV/ETH",
        [JsonProperty("symbol")]
        public string symbol { get; set; }

        //	"type": "limit",
        [JsonProperty("type")]
        public string type { get; set; }

        //	"side": "buy",
        [JsonProperty("side")]
        public string side { get; set; }

        //	"amount": 3954.17203437,
        [JsonProperty("amount")]
        public decimal amount { get; set; }

        //	"price": 0.0005914,
        [JsonProperty("price")]
        public decimal price { get; set; }

        //	"cost": 2.3384973411264176,
        [JsonProperty("cost")]
        public decimal cost { get; set; }

        //	"status": "open"
        [JsonProperty("status")]
        public string status { get; set; }
    }
}
