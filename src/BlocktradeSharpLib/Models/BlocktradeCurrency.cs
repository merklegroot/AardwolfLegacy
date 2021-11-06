using Newtonsoft.Json;

namespace BlocktradeExchangeLib.Models
{
    public class BlocktradeCurrency
    {
        // "id": 3,
        [JsonProperty("id")]
        public int Id { get; set; }

        // "name": "BTC"
        [JsonProperty("name")]
        public string Name { get; set; }

    }
}
