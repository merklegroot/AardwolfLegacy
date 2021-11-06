using Newtonsoft.Json;
using System;

namespace hitbtc_lib.Client.ClientModels
{
    public class HitBtcClientTransactionItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("index")]
        public long Index { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("amount")]
        public decimal? Amount { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("fee")]
        public decimal? Fee { get; set; }
    }
}
