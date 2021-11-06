using Newtonsoft.Json;
using System.Collections.Generic;

namespace blocktrade_lib.Models
{
    public class BlockTradeOrderBook
    {
        [JsonProperty("bids")]
        public List<BlockTradeOrder> Bids { get; set; }

        [JsonProperty("asks")]
        public List<BlockTradeOrder> Asks { get; set; }        
    }
}
