using Newtonsoft.Json;

namespace KucoinClientModelLib
{
    public class KucoinClientTradeHistoryItem
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("createdAt")]
        public long CreatedAt { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("side")]
        public string Side { get; set; }

        [JsonProperty("dealValue")]
        public decimal DealValue { get; set; }

        [JsonProperty("fee")]
        public decimal Fee { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("dealPrice")]
        public decimal DealPrice { get; set; }
    }
}
