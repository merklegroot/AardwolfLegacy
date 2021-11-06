using Newtonsoft.Json;

namespace kucoin_lib.Models
{
    // {"info":{"success":true,"code":"OK",
    // "msg":"Operation succeeded.",
    // "timestamp":1527254677824,"data":null}}
    public class KucoinWithdrawalResponse
    {
        [JsonProperty("info")]
        public KucoinWithdrawalResponseInfo Info { get; set; }

        public class KucoinWithdrawalResponseInfo
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("code")]
            public string Code { get; set; }

            [JsonProperty("msg")]
            public string Msg { get; set; }

            [JsonProperty("timestamp")]
            public long TimeStamp { get; set; }
        }
    }
}
