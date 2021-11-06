using Newtonsoft.Json;

namespace KucoinClientModelLib
{
    public class KucoinClientResponse
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

        // "timestamp": 1541784149506,
        [JsonProperty("timestamp")]
        public long TimeStamp { get; set; }
    }

    public class KucoinClientResponse<T> : KucoinClientResponse
    {
        [JsonProperty("data")]
        public T Data { get; set; }
    }
}
