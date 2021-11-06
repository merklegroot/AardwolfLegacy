using Newtonsoft.Json;
using System.Collections.Generic;

namespace gemini_lib.Models
{
    public class GeminiOrderBook
    {
        [JsonProperty("bids")]
        public List<GeminiOrder> Bids { get; set; }

        [JsonProperty("asks")]
        public List<GeminiOrder> Asks { get; set; }
    }
}
