using Newtonsoft.Json;

namespace livecoin_lib.Models
{
    public class LivecoinCcxtPlaceLimitOrdersResponse
    {
        [JsonProperty("info")]
        public ResponseInfo Info { get; set; }

        //"id": "28764841401",
        [JsonProperty("id")]
        public long Id { get; set; }

        //"status": "open"
        [JsonProperty("status")]
        public string Status { get; set; }

        public class ResponseInfo
        {
            //	"success": true,
            [JsonProperty("success")]
            public bool Success { get; set; }

            //	"added": true,
            [JsonProperty("added")]
            public bool Added { get; set; }

            //	"orderId": 28764841401,
            [JsonProperty("orderId")]
            public long OrderId { get; set; }

            //	"exception": null
            [JsonProperty("exception")]
            public string Exception { get; set; }
        }
    }
}
