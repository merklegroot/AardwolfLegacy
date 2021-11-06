using Newtonsoft.Json;
using System.Collections.Generic;

namespace bit_z_lib.Models
{
    public class BitzOpenOrdersInfo
    {
        [JsonProperty("data")]
        public List<BitzOpenOrdersInfoDataItem> Data { get; set; }

        [JsonProperty("pageInfo")]
        public BitzPageInfo PageInfo { get; set; }
    }
}
