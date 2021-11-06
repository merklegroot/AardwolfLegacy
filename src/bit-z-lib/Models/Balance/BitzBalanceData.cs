using Newtonsoft.Json;
using System.Collections.Generic;

namespace bit_z_lib.Models.Balance
{
    public class BitzBalanceData
    {
        [JsonProperty("cny")]
        public decimal? Cny { get; set; }

        [JsonProperty("usd")]
        public decimal? Usd { get; set; }

        [JsonProperty("btc_total")]
        public decimal? BtcTotal { get; set; }

        [JsonProperty("info")]
        public List<BitzBalanceInfoItem> Info { get; set; }
    }
}
