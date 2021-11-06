using Newtonsoft.Json;

namespace qryptos_lib.Models
{
    public class QryptosFee
    {
        [JsonProperty("cost")]
        public decimal? Cost { get; set; }
    }
}
