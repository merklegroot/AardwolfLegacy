using Newtonsoft.Json;
using System;

namespace coinbase_lib.Models
{
    public class CoinbaseTransaction
    {
        public Guid Id { get; set; }

        public string Type { get; set; }

        public string Status { get; set; }

        public CoinbaseAmount Amount { get; set; }

        [JsonProperty("native_amount")]
        public CoinbaseAmount NativeAmount { get; set; }

        public string Description { get; set; }

        [JsonProperty("Created_At")]
        public DateTime? CreatedAt { get; set; }

        [JsonProperty("Updated_At")]
        public DateTime? UpdatedAt { get; set; }

        public string Resource { get; set; }

        [JsonProperty("Resource_Path")]
        public string ResourcePath { get; set; }

        public CoinbaseTransactionDetails Details { get; set; }

        public CoinbaseNetwork Network { get; set; }

        public CoinbaseDestination To { get; set; }
    }
}
