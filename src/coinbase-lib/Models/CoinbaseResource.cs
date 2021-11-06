using Newtonsoft.Json;
using System;

namespace coinbase_lib.Models
{
    public class CoinbaseResource
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("resource")]
        public string Resource { get; set; }

        [JsonProperty("resource_path")]
        public string ResourcePath { get; set; }
    }
}
