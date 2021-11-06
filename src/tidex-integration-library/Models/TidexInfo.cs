using Newtonsoft.Json;
using System.Collections.Generic;

namespace tidex_integration_library.Models
{
    public class TidexInfo
    {
        // {"server_time":1521221595,"pairs":{"ltc_btc":{"decimal_places":8,"min_price":0.00000001,"max_price":3.0,"min_amount":0.001,"max_amount":1000000.0,"min_total":0.0001,"hidden":0,"fee":0.1}
        [JsonProperty("server_time")]
        public long ServerTime { get; set; }

        [JsonProperty("pairs")]
        public Dictionary<string, object> PairsDictionary { get; set; }

        public List<TixedPairInfo> Pairs { get; set; }

        /// <summary>
        /// Success: property doesn't exist
        /// Failure: 0
        /// </summary>
        // "success":0
        [JsonProperty("success")]
        public decimal? Success { get; set; }

        // "error":"not available"
        [JsonProperty("error")]        
        public string Error { get; set; }
    }
}
