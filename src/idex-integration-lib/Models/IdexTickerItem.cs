using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace idex_integration_lib.Models
{
    public class IdexTickerItem
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("baseSymbol")]
        public string BaseSymbol { get; set; }

        [JsonProperty("last")]
        public decimal? Last { get; set; }

        [JsonProperty("high")]
        public decimal? High { get; set; }

        [JsonProperty("low")]
        public decimal? Low { get; set; }

        [JsonProperty("lowestAsk")]
        public decimal? LowestAsk { get; set; }

        [JsonProperty("highestBid")]
        public decimal? HighestBid { get; set; }

        [JsonProperty("percentChange")]
        public decimal? PercentChange { get; set; }

        [JsonProperty("baseVolume")]
        public decimal? BaseVolume { get; set; }

        [JsonProperty("quoteVolume")]
        public decimal? QuoteVolume { get; set; }
    }
}
