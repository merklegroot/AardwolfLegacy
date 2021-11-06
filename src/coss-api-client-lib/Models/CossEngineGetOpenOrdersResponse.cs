using Newtonsoft.Json;
using System.Collections.Generic;

namespace coss_api_client_lib.Models
{
    public class CossEngineGetOpenOrdersResponse
    {
        // "total": 1,
        [JsonProperty("total")]
        public int Total { get; set; }

        
        // "list": [{
        [JsonProperty("list")]
        public List<CossEngineOpenOrder> List { get; set; }        
    }
}
