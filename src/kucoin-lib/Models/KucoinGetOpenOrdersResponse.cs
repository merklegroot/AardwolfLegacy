using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kucoin_lib.Models
{
    public class KucoinGetOpenOrdersResponse
    {
	    // "success": true,
        [JsonProperty("success")]
        public string Success { get; set; }

        // "code": "OK",
        [JsonProperty("code")]
        public string Code { get; set; }

        // "msg": "Operation succeeded."
        [JsonProperty("msg")]
        public string Msg { get; set; }

        // "timestamp": 1539399921752,
        [JsonProperty("timestamp")]
        public long TimeStamp { get; set; }

        // "data": {
        [JsonProperty("data")]
        public ResponseData Data { get; set; }

        public class ResponseData
        {
            // "SELL": [],
            [JsonProperty("SELL")]
            public List<KucoinOpenOrder> Sell { get; set; }

            // "BUY": [{
            [JsonProperty("BUY")]
            public List<KucoinOpenOrder> Buy { get; set; }
        }        
    }
}
