using Newtonsoft.Json;

namespace coss_api_client_lib.Models
{
    public class CreateApiOrderRequestMessage
    {
        [JsonProperty("order_symbol")]
        public string OrderSymbol { get; set; }

        [JsonProperty("order_price")]
        public string OrderPrice { get; set; }

        [JsonProperty("order_side")]
        public string OrderSide { get; set; }

        [JsonProperty("order_size")]
        public string OrderSize { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("timestamp")]
        public string TimeStamp { get; set; }

        [JsonProperty("recvWindow")]
        public long RecvWindow { get; set; }
    }
}
