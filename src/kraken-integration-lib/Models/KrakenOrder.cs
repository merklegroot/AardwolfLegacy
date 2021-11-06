using Newtonsoft.Json;

namespace kraken_integration_lib.Models
{
    namespace kraken_lib.Models
    {
        public class KrakenOrder
        {
            [JsonProperty("misc")]
            public string Misc { get; set; }
            
            [JsonProperty("price")]
            public decimal Price { get; set; }
            
            [JsonProperty("fee")]
            public decimal Fee { get; set; }
            
            [JsonProperty("ordertxid")]
            public string OrderTxId { get; set; }

            [JsonProperty("cost")]
            public decimal Cost { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("vol")]
            public decimal Vol { get; set; }

            [JsonProperty("time")]
            public decimal Time { get; set; }

            [JsonProperty("ordertype")]
            public string Ordertype { get; set; }

            [JsonProperty("pair")]
            public string Pair { get; set; }

            [JsonProperty("margin")]
            public decimal Margin { get; set; }
        }
    }
}
