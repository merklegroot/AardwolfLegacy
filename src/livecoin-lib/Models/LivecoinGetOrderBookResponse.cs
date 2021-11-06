using Newtonsoft.Json;
using System.Collections.Generic;

namespace livecoin_lib.Models
{
    public class LivecoinGetOrderBookResponse
    {
        // "timestamp": 1540014927179,
        [JsonProperty("timestamp")]
        public long TimeStamp { get; set; }

        // "asks": [["0.03145998", "0.07057939", 1540014911903], ...
        [JsonProperty("asks")]
        public List<List<string>> Asks { get; set; }

        // "bids": [["0.0314", "0.00770394", 1540014282576], ...
        [JsonProperty("bids")]
        public List<List<string>> Bids { get; set; }       
    }
}
