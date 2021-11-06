using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bit_z_lib.Models
{
    public class BitzSymbolListResponse
    {
        // "status": 200,
        [JsonProperty("status")]
        public int Status { get; set; }

        // "msg": "",
        [JsonProperty("msg")]
        public object Message { get; set; }

        // "data": {
        [JsonProperty("data")]
        public Dictionary<string, BitzSymbol> Data { get; set; }
    }
}
