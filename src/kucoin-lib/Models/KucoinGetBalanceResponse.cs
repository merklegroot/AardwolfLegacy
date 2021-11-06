using Newtonsoft.Json;
using System.Collections.Generic;

namespace kucoin_lib.Models
{
    public class KucoinGetBalanceResponse
    {
        // {"info":[{"coinType":"KCS","balanceStr":"0.0","freezeBalance":0,"balance":0,"freezeBalanceStr":"0.0"},{"coinType":"NEO","balanceStr":"0.0","freezeBalance":0,"balance":0,"freezeBalanceStr":"0.0"},
        [JsonProperty("info")]
        public List<KucoinGetBalanceResponseInfoItem> Info { get; set; }

        public class KucoinGetBalanceResponseInfoItem
        {
            // {"coinType":"KCS","balanceStr":"0.0","freezeBalance":0,"balance":0,"freezeBalanceStr":"0.0"}
            [JsonProperty("coinType")]
            public string CoinType { get; set; }

            [JsonProperty("balanceStr")]
            public string BalanceStr { get; set; }

            [JsonProperty("freezeBalanceStr")]
            public string FreezeBalanceStr { get; set; }

            [JsonProperty("balance")]
            public decimal? Balance { get; set; }

            [JsonProperty("freezeBalance")]
            public decimal? FreezeBalance { get; set; }           
        }
    }
}
