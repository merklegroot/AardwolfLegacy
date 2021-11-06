using Newtonsoft.Json;

namespace kucoin_lib.Models
{
    public class KucoinGetDepositAddressResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }

        [JsonProperty("timestamp")]
        public long TimeStamp { get; set; }

        [JsonProperty("data")]
        public ResponseData Data { get; set; }

        public class ResponseData
        {
            [JsonProperty("oid")]
            public string Oid { get; set; }

            [JsonProperty("address")]
            public string Address { get; set; }

            [JsonProperty("context")]
            public object Context { get; set; }

            [JsonProperty("userOid")]
            public string UserOid { get; set; }

            [JsonProperty("coinType")]
            public string CoinType { get; set; }

            [JsonProperty("createdAt")]
            public string CreatedAt { get; set; }

            [JsonProperty("deletedAt")]
            public string DeletedAt { get; set; }

            [JsonProperty("updatedAt")]
            public string UpdatedAt { get; set; }

            [JsonProperty("lastReceivedAt")]
            public string LastReceivedAt { get; set; }
        }
    }
}
