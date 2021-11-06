using Newtonsoft.Json;

namespace oex_lib.Models
{
    public class OexOrder
    {
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }
    }
}
