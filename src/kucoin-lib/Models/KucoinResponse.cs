using Newtonsoft.Json;

namespace kucoin_lib.Models
{
    public class KucoinResponse
    {
        // "success": true,
        [JsonProperty("success")]
        public bool Success { get; set; }

        // "code": "OK",
        [JsonProperty("code")]
        public string Code { get; set; }

        // "msg": "Operation succeeded.",
        [JsonProperty("msg")]
        public string Msg { get; set; }

        // "timestamp": 1523907547485,
        [JsonProperty("timestamp")]
        public long TimeStamp { get; set; }
    }

    public class KucoinResponse<T> : KucoinResponse
    {
        [JsonProperty("data")]
        public T Data { get; set; }
    }
}
