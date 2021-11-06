using Newtonsoft.Json;

namespace KucoinClientModelLib
{
    public class KucoinClientOrderIdPayload
    {
        [JsonProperty("orderOid")]
        public string OrderOid { get; set; }
    }
}
