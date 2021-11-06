using Newtonsoft.Json;

namespace KucoinClientModelLib
{
    public class KucoinClientSymbol
    {
        //"symbol": "REQ-ETH",
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        //"quoteMaxSize": "99999999",
        [JsonProperty("quoteMaxSize")]
        public decimal quoteMaxSize { get; set; }

        //"enableTrading": true,
        [JsonProperty("enableTrading")]
        public string enableTrading { get; set; }

        //"priceIncrement": "0.0000001",
        [JsonProperty("priceIncrement")]
        public decimal priceIncrement { get; set; }

        //"baseMaxSize": "1000000",
        [JsonProperty("baseMaxSize")]
        public string baseMaxSize { get; set; }

        //"baseCurrency": "REQ",
        [JsonProperty("baseCurrency")]
        public string BaseCurrency { get; set; }

        //"quoteCurrency": "ETH",
        [JsonProperty("quoteCurrency")]
        public string QuoteCurrency { get; set; }

        //"market": "ETH",
        [JsonProperty("market")]
        public string market { get; set; }

        //"quoteIncrement": "0.0000001",
        [JsonProperty("quoteIncrement")]
        public decimal quoteIncrement { get; set; }

        //"baseMinSize": "1",
        [JsonProperty("baseMinSize")]
        public decimal baseMinSize { get; set; }

        //"quoteMinSize": "0.00001",
        [JsonProperty("quoteMinSize")]
        public decimal quoteMinSize { get; set; }

        //"name": "REQ-ETH",
        [JsonProperty("name")]
        public string name { get; set; }

        //"baseIncrement": "0.0001"
        [JsonProperty("baseIncrement")]
        public decimal baseIncrement { get; set; }
    }
}
