using Newtonsoft.Json;

namespace bit_z_lib.Models
{
    public class BitzSymbol
    {
        // "id": "1",
        [JsonProperty("id")]
        public int Id { get; set; }

        // "name": "ltc_btc",
        [JsonProperty("name")]
        public string Name { get; set; }

        // "coinFrom": "ltc",
        [JsonProperty("coinFrom")]
        public string CoinFrom { get; set; }

        // "coinTo": "btc",
        [JsonProperty("coinTo")]
        public string CoinTo { get; set; }

        // "numberFloat": "4",
        [JsonProperty("numberFloat")]
        public int? NumberFloat { get; set; }

        // "priceFloat": "8",
        [JsonProperty("priceFloat")]
        public int? PriceFloat { get; set; }

        // "status": "1",
        [JsonProperty("status")]
        public int? Status { get; set; }

        // "minTrade": "0.010",
        [JsonProperty("minTrade")]
        public decimal? MinTrade { get; set; }

        // "maxTrade": "500000000.000"
        [JsonProperty("maxTrade")]
        public decimal? MaxTrade { get; set; }
    }
}
