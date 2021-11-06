using Newtonsoft.Json;

namespace hitbtc_lib.Models
{
    public class HitBtcClientCreateOrderRequest
    {
        // 'symbol':'ethbtc', 
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        // 'side': 'sell', 
        [JsonProperty("side")]
        public string Side { get; set; }

        // 'quantity': '0.063', 
        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        // 'price': '0.046016'
        [JsonProperty("price")]
        public decimal Price { get; set; }
    }
}
