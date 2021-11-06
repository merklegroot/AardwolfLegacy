using Newtonsoft.Json;

namespace bit_z_lib.Models
{
    public class BitzMarket
    {
        //"id": "ltc_btc",
        [JsonProperty("id")]
        public string Id { get; set; }

        //"symbol": "LTC/BTC",
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        //"base": "LTC",
        [JsonProperty("base")]
        public string BaseSymbol { get; set; }

        //"quote": "BTC",
        [JsonProperty("quote")]
        public string QuoteSymbol { get; set; }

        //"baseId": "ltc",
        [JsonProperty("baseId")]
        public string BaseId { get; set; }

        //"quoteId": "btc",
        [JsonProperty("quoteId")]
        public string QuoteId { get; set; }

        //"active": true,
        [JsonProperty("active")]
        public string Active { get; set; }

        //"info": {
        [JsonProperty("info")]
        public BitzMarketInfo Info { get; set; }

        public class BitzMarketInfo
        {
            //	"date": 1532117112,
            [JsonProperty("date")]
            public long? Date { get; set; }

            //	"last": "0.01115400",
            [JsonProperty("last")]
            public double? Last { get; set; }

            //	"buy": "0.01109367",
            [JsonProperty("buy")]
            public double? Buy { get; set; }

            //	"sell": "0.01121534",
            [JsonProperty("sell")]
            public double? Sell { get; set; }

            //	"high": "0.01167586",
            [JsonProperty("high")]
            public double? High { get; set; }

            //	"low": "0.01110300",
            [JsonProperty("low")]
            public double? Low { get; set; }

            //	"vol": "114590.2419"
            [JsonProperty("vol")]
            public double? Vol { get; set; }
        }
    }
}
