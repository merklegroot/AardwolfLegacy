using Newtonsoft.Json;
using System.Collections.Generic;

namespace kucoin_lib.Models
{
    public class KucoinOrderData
    {
        [JsonProperty("SELL")]
        public List<List<string>> Asks { get; set; }

        [JsonProperty("BUY")]
        public List<List<string>> Bids { get; set; }
    }
}
