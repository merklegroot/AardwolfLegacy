using Newtonsoft.Json;

namespace kucoin_lib.Models
{
    public class KucoinOpenOrder
    {
        // "oid": "5bc160e1bf25ad3ad96f2803",
        [JsonProperty("oid")]
        public string Oid { get; set; }

        // "userOid": "5ad4f8f93f705c1299ca0bdc",
        [JsonProperty("userOid")]
        public string UserOid { get; set; }

        // "coinType": "ETH",
        [JsonProperty("coinType")]
        public string CoinType { get; set; }

        // "coinTypePair": "BTC",
        [JsonProperty("coinTypePair")]
        public string CoinTypePair { get; set; }

        // "direction": "BUY",
        [JsonProperty("direction")]
        public string Direction { get; set; }

        // "price": 0.03143556,
        [JsonProperty("price")]
        public decimal? Price { get; set; }

        //"dealAmount": 0.0,
        [JsonProperty("dealAmount")]
        public decimal? DealAmount { get; set; }

        // "pendingAmount": 0.01,
        [JsonProperty("pendingAmount")]
        public decimal? PendingAmount { get; set; }

        // "dealValue": 0.0,
        [JsonProperty("dealValue")]
        public decimal? DealValue { get; set; }

        // "dealAveragePrice": 0.0,
        [JsonProperty("dealAveragePrice")]
        public decimal? DealAveragePrice { get; set; }

        // "createdAt": 1539399906000,
        [JsonProperty("createdAt")]
        public long? CreatedAt { get; set; }

        // "updatedAt": 1539399906000
        [JsonProperty("updatedAt")]
        public long? UpdatedAt { get; set; }
    }
}
