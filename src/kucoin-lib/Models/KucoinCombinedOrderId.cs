using Newtonsoft.Json;

namespace kucoin_lib.Models
{
    public class KucoinCombinedOrderId
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("isBid")]
        public bool IsBid { get; set; }

        [JsonProperty("nativeSymbol")]
        public string NativeSymbol { get; set; }

        [JsonProperty("nativeBaseSymbol")]
        public string NativeBaseSymbol { get; set; }
    }
}
