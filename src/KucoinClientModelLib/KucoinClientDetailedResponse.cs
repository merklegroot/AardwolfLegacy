using Newtonsoft.Json;

namespace KucoinClientModelLib
{
    public class KucoinClientDetailedResponse
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

    public class KucoinClientDetailedResponse<T> : KucoinClientDetailedResponse
    {
        [JsonProperty("data")]
        public T Data { get; set; }
    }
}
