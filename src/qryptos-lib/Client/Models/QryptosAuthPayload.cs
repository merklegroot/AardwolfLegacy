using Newtonsoft.Json;

namespace qryptos_lib.Client
{
    public class QryptosAuthPayload
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("nonce")]
        public string Nonce { get; set; }

        [JsonProperty("token_id")]
        public string TokenId { get; set; }
    }
}
