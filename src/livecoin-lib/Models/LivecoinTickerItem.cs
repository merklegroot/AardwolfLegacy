using Newtonsoft.Json;

namespace livecoin_lib.Models
{
    public class LivecoinTickerItem
    {
	    // "cur": "BNT",
        [JsonProperty("cur")]
        public string Cur { get; set; }

        //"symbol": "BNT/BTC",
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        //"last": 0,
        [JsonProperty("last")]
        public decimal? Last { get; set; }

        // "high": 0,
        [JsonProperty("high")]
        public decimal? High { get; set; }

        // "low": 0,
        [JsonProperty("low")]
        public decimal? Low { get; set; }

        // "volume": 0E-8,
        [JsonProperty("volume")]
        public decimal? Volume { get; set; }

        // "vwap": 0,
        [JsonProperty("vwap")]
        public decimal? Vwap { get; set; }

        // "max_bid": 0.00014001,
        [JsonProperty("max_bid")]
        public decimal? MaxBid { get; set; }

        // "min_ask": 0.00015917,
        [JsonProperty("min_ask")]
        public decimal? MinAsk { get; set; }

        // "best_bid": 0.00014001,
        [JsonProperty("best_bid")]
        public decimal? BestBid { get; set; }

        //" best_ask": 0.00015917
        [JsonProperty("best_ask")]
        public decimal? BestAsk { get; set; }
    }
}
