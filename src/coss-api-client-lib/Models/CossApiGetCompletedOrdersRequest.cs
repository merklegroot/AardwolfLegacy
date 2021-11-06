using Newtonsoft.Json;

namespace coss_api_client_lib.Models
{
    internal class CossApiGetCompletedOrdersRequest
    {
        //"symbol": "eth-btc",
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        //"from_id": "order id to fetch from",
        [JsonProperty("from_id")]
        public string FromId { get; set; }

        //"limit": "default and maximum is 50",
        [JsonProperty("limit")]
        public int? Limit { get; set; }

        //"page": default is 0,
        [JsonProperty("page")]
        public int? Page { get; set; }

        //"timestamp": 1530682938651,
        [JsonProperty("timestamp")]
        public string TimeStamp { get; set; }

        //"recvWindow": 5000
        [JsonProperty("recvWindow")]
        public long RecvWindow { get; set; }
    }
}
