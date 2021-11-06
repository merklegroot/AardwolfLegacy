using Newtonsoft.Json;

namespace gemini_lib.Models
{
    public class GeminiOrder
    {
        // "price": "196.52",
        [JsonProperty("price")]
        public decimal Price { get; set; }

        // "amount": "20",
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        // "timestamp": "1541040885"
        [JsonProperty("timestamp")]
        public long TimeStamp { get; set; }
    }
}
