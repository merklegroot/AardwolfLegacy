using Newtonsoft.Json;
using System.Collections.Generic;

namespace BlocktradeExchangeLib.Models
{
    public class BlocktradeGetUserOrdersResponse
    {
        [JsonProperty("data")]
        public List<BlocktradeUserOrder> Data { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("offset")]
        public int Offset { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }
    }
}
