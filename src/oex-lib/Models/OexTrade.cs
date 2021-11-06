using Newtonsoft.Json;

namespace oex_lib.Models
{
    public class OexTrade : OexOrder
    {
        // "time": "15:37:13",
        [JsonProperty("time")]
        public string Time { get; set; }

        // "en_type": "ask",
        [JsonProperty("en_type")]
        public string EnType { get; set; }

        // "type": "卖出"
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
