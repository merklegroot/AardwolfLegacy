using Newtonsoft.Json;
using System.Collections.Generic;

namespace idex_integration_lib.Models
{
    public class IdexTradeHistoryMeta
    {
        [JsonProperty("trades")]
        public Dictionary<string, List<IdexTradeHistoryItem>> Trades { get; set; }        
    }
}
