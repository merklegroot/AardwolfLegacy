using Newtonsoft.Json;

namespace KucoinClientModelLib
{
    public class KucoinClientSimpleResponse<T>
    {
        [JsonProperty("code")]
        public long Code { get; set; }

        [JsonProperty("data")]
        public KucoinClientPagedData<T> Data { get; set; }
    }
}
