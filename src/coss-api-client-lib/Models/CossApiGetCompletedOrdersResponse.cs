using Newtonsoft.Json;
using System.Collections.Generic;

namespace coss_api_client_lib.Models
{
    public class CossApiGetCompletedOrdersResponse
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("list")]
        public List<CossApiCompletedOrder> List { get; set; }
    }
}
