using Newtonsoft.Json;

namespace kraken_integration_lib.Models
{
    public class KrakenLedgerItem
    {
        [JsonProperty("refid")]
        public string Refid { get; set; }

        [JsonProperty("time")]
        public decimal Time { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("aclass")]
        public string Aclass { get; set; }

        [JsonProperty("asset")]
        public string Asset { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("fee")]
        public decimal Fee { get; set; }

        [JsonProperty("balance")]
        public decimal Balance { get; set; }
    }
}
