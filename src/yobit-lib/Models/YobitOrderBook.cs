using Newtonsoft.Json;
using System.Collections.Generic;

namespace yobit_lib.Models
{
    public class YobitOrderBook
    {
        [JsonProperty("asks")]
        public List<List<decimal>> Asks { get; set; }

        [JsonProperty("bids")]
        public List<List<decimal>> Bids { get; set; }
    }
}
