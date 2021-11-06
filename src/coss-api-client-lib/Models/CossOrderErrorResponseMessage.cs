using Newtonsoft.Json;

namespace coss_api_client_lib.Models
{
    public class CossOrderErrorResponseMessage
    {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("error_description")]
        public string ErrorDescription { get; set; }
    }
}
