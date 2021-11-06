using Newtonsoft.Json;

namespace hitbtc_lib.Models
{
    public class HitBtcCcxtBalanceItem
    {
        public string Symbol { get; set; }

		// "free": 0,
        [JsonProperty("free")]
        public decimal? Free { get; set; }

        // "used": 0,
        [JsonProperty("used")]
        public decimal? Used { get; set; }

        // "total": 0
        [JsonProperty("total")]
        public decimal? Total { get; set; }
    }
}
