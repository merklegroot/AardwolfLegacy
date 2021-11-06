using Newtonsoft.Json;
using System.Collections.Generic;

namespace coss_api_client_lib.Models
{
    public class CossEngineOrderBook
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("asks")]
        public List<CossEngineOrder> Asks { get; set; }

        [JsonProperty("bids")]
        public List<CossEngineOrder> Bids { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }

        [JsonProperty("time")]
        public long Time { get; set; }

        public class CossEngineOrder : List<decimal>
        {
            [JsonIgnore]
            public decimal Price
            {
                get
                {
                    return this[0];
                }
            }

            [JsonIgnore]
            public decimal Quantity
            {
                get
                {
                    return this[1];
                }
            }
        }
    }
}
