using Newtonsoft.Json;

namespace iridium_lib.Models
{
    internal class ExchangeServiceModel
    {
        [JsonProperty("exchange")]
        public string Exchange { get; set; }
    }
}
