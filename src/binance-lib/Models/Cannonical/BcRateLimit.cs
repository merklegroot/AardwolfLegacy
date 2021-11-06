using Newtonsoft.Json;

namespace binance_lib.Models.Canonical
{
    public class BcRateLimit
    {
        [JsonProperty("rateLimitType")]
        public string RateLimitType { get; set; }

        [JsonProperty("interval")]
        public string Interval { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }
    }
}
