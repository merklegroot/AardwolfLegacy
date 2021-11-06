using Newtonsoft.Json;

namespace oex_lib.Models
{
    public class OexTradingPair
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("baseSymbol")]
        public string BaseSymbol { get; set; }
    }
}
