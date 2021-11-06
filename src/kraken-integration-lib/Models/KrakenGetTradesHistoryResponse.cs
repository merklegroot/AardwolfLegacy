using kraken_integration_lib.Models.kraken_lib.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace kraken_integration_lib.Models
{
    public class KrakenGetTradesHistoryResponse
    {
        [JsonProperty("error")]
        public List<object> Error { get; set; }

        [JsonProperty("result")]
        public ResponseResult Result { get; set; }

        public class ResponseResult
        {
            [JsonProperty("trades")]
            public Dictionary<string, KrakenOrder> Trades { get; set; }
        }
    }
}
