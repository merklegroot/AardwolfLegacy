using Newtonsoft.Json;
using System;
using System.Globalization;

namespace fork_delta_integration.Models
{
    public class ForkDeltaTickerItem
    {
	    // "tokenAddr": "0x940d73c91db9f82440702f6cc8323a8c60583777",
        [JsonProperty("tokenAddr")]
        public string TokenAddress { get; set; }

        // "quoteVolume": "0",
        [JsonProperty("quoteVolume")]
        public string QuoteVolumeText { get; set; }

        //public decimal? QuoteVolume
        //{
        //    get
        //    {
        //        return decimal.TryParse(QuoteVolumeText, NumberStyles.AllowExponent, null, out decimal value)
        //            ? value: (decimal?)null;
        //    }
        //}

        // "baseVolume": "0",
        [JsonProperty("baseVolume")]
        public string BaseVolumeText { get; set; }

        // "last": null,
        [JsonProperty("last")]
        public string LastText { get; set; }

        // "bid": "0.0000110000",
        [JsonProperty("bid")]
        public string BidText { get; set; }

        //  "ask": null,
        [JsonProperty("ask")]
        public string AskText { get; set; }

        // "updated": "2018-02-28T20:16:52.772658"
        [JsonProperty("updated")]
        public DateTime? updated { get; set; }
    }
}
