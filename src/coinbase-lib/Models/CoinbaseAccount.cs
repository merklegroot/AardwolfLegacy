using Newtonsoft.Json;
using System;

namespace coinbase_lib.Models
{
    public class CoinbaseAccount
    {
        // [JsonProperty("Client")]
        // public object Client { get; set; }

        [JsonProperty("Id")]
        public string Id { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Primary")]
        public bool Primary { get; set; }

        [JsonProperty("Type")]
        public string Type { get; set; }

        [JsonProperty("Currency")]
        public string Currency { get; set; }

        [JsonProperty("Balance")]
        public CoinbaseAmount Balance { get; set; }

        [JsonProperty("Created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("Updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("Resource")]
        public string Resource { get; set; }

        [JsonProperty("Resource_path")]
        public string ResourcePath { get; set; }

        [JsonProperty("Native_balance")]
        public CoinbaseAmount NativeBalance { get; set; }
    }
}
