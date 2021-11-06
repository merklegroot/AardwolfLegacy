using Newtonsoft.Json;

namespace coss_lib.Models
{
    //public class CossCancelOrderResponse
    //{
    //    // "successful":true
    //    [JsonProperty("successful")]
    //    public bool Successful { get; set; }

    //    // "payload":"c373a5ae-4ade-4631-b068-701e889e7b5f"
    //    [JsonProperty("payload")]
    //    public string Payload { get; set; }
    //}

    public class CossCancelOrderResponse
    {
	    // "order_symbol": "ETH_BTC",
        [JsonProperty("order_symbol")]
        public string OrderSymbol { get; set; }

        // "order_id": "f124d796-c7d3-4a94-890a-bd3921c4d1a5",
        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        // "order_size": 0,
        [JsonProperty("order_size")]
        public int? OrderSize { get; set; }

        // "account_id": "95e0fffe-6014-4414-8c51-d8a96e12b980",\
        [JsonProperty("account_id")]
        public string AccountId { get; set; }

        // "timestamp": 1544665150171,
        [JsonProperty("timestamp")]
        public long TimeStamp { get; set; }

        // "recvWindow": 5000
        [JsonProperty("recvWindow")]
        public int? RecvWindow { get; set; }
    }
}
