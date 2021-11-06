using Newtonsoft.Json;

namespace BlocktradeExchangeLib.Models
{
    public class BlocktradeOrder
    {
        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }
    }
}
