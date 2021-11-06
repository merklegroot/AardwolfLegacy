using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coss_api_client_lib.Models
{
    public class CossApiGetOpenOrdersResponseMessage
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("list")]
        public List<CossApiOpenOrder> List { get; set; }
    }
}
