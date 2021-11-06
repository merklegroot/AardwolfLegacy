using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kucoin_lib.Models
{
    public class KucoinGetOpenOrdersForTradingPairResponseItem
    {
        [JsonProperty("info")]
        public ResponseInfo Info { get; set; }

        public class ResponseInfo
        {
            // oid: '5babb5d5ae84284de24966de',
            [JsonProperty("oid")]
            public string Oid { get; set; }

            // userOid: '5ad4f8f93f705c1299ca0bdc',
            [JsonProperty("userOid")]
            public string UserOid { get; set; }

            // coinType: 'CS',
            [JsonProperty("coinType")]
            public string CoinType { get; set; }

            // coinTypePair: 'ETH',
            [JsonProperty("coinTypePair")]
            public string CoinTypePair { get; set; }

            // direction: 'BUY',
            [JsonProperty("direction")]
            public string Direction { get; set; }

            // price: 0.0007775,
            [JsonProperty("price")]
            public decimal? Price { get; set; }

            // dealAmount: 0,
            [JsonProperty("dealAmount")]
            public decimal? DealAmount { get; set; }

            // pendingAmount: 447.9311,
            [JsonProperty("pendingAmount")]
            public decimal? PendingAmount { get; set; }

            // dealValue: 0,
            [JsonProperty("dealValue")]
            public decimal? DealValue { get; set; }

            // dealAveragePrice: 0,
            [JsonProperty("dealAveragePrice")]
            public decimal? DealAveragePrice { get; set; }

            // createdAt: 1537979862000,
            [JsonProperty("createdAt")]
            public long? CreatedAt { get; set; }

            // updatedAt: 1537979862000,
            [JsonProperty("updatedAt")]
            public long? UpdatedAt { get; set; }

            // status: 'open'
            [JsonProperty("status")]
            public string Status { get; set; }
        }

        // id: '5babb5d5ae84284de24966de',
        [JsonProperty("id")]
        public string Id { get; set; }

        // timestamp: 1537979862000,
        [JsonProperty("timestamp")]
        public long? Timestamp { get; set; }

        // datetime: '2018-09-26T16:37:42.000Z',
        [JsonProperty("datetime")]
        public DateTime? Datetime { get; set; }

        // lastTradeTimestamp: undefined,
        [JsonProperty("lastTradeTimestamp")]
        public DateTime? LastTradeTimestamp { get; set; }

        // symbol: 'CS/ETH',
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        // type: 'limit',
        [JsonProperty("type")]
        public string Type { get; set; }

        // side: 'buy',
        [JsonProperty("side")]
        public string Side { get; set; }

        // price: 0.0007775,
        [JsonProperty("price")]
        public decimal? Price { get; set; }

        // amount: 447.9311,
        [JsonProperty("amount")]
        public decimal? Amount { get; set; }

        // cost: 0.34826643025,
        [JsonProperty("cost")]
        public decimal? Cost { get; set; }

        // filled: 0,
        [JsonProperty("filled")]
        public decimal? Filled { get; set; }

        // remaining: 447.9311,
        [JsonProperty("remaining")]
        public decimal? Remaining { get; set; }

        // status: 'open',
        [JsonProperty("status")]
        public string Status { get; set; }

        //fee: { cost: undefined, rate: undefined, currency: 'CS' },
        //trades: undefined }
    }
}
