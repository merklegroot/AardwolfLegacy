using Newtonsoft.Json;

namespace bit_z_lib.Models.Balance
{
    public class BitzBalanceInfoItem
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("num")]
        public decimal? Num { get; set; }

        [JsonProperty("over")]
        public decimal? Over { get; set; }

        [JsonProperty("lock")]
        public decimal? Lock { get; set; }

        [JsonProperty("btc")]
        public decimal? Btc { get; set; }

        [JsonProperty("usd")]
        public decimal? Usd { get; set; }

        [JsonProperty("cny")]
        public decimal? Cny { get; set; }
    }
}
