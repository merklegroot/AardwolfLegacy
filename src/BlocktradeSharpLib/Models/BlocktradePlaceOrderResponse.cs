using Newtonsoft.Json;

namespace BlocktradeExchangeLib.Models
{
    public class BlocktradePlaceOrderResponse
    {
        [JsonProperty("id")]
        public long Id { get; set; }
    }
}
