using Newtonsoft.Json;
using System.Collections.Generic;

namespace BlocktradeExchangeLib.Models
{
    public class BlocktradeOrderBook
    {
        [JsonProperty("bids")]
        public List<BlocktradeOrder> Bids { get; set; }

        [JsonProperty("asks")]
        public List<BlocktradeOrder> Asks { get; set; }        
    }
}
