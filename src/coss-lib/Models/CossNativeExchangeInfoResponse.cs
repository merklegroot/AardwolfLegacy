using Newtonsoft.Json;

namespace coss_lib.Models
{
    public class CossNativeExchangeInfoResponse
    {
        // "timezone": "UTC",
        [JsonProperty("timezone")]
        public string TimeZone { get; set; }

        // "server_time": 1541968617943,
        [JsonProperty("server_time")]
        public long ServerTime { get; set; }

	    // "rate_limits": [],

    }
}
