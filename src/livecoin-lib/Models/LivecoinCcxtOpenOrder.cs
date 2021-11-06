using Newtonsoft.Json;

namespace livecoin_lib.Models
{
    public class LivecoinCcxtOpenOrder
    {
        // id: 28768684301,
        [JsonProperty("id")]
        public long Id { get; set; }

        // timestamp: 1544118253667,
        [JsonProperty("timestamp")]
        public long TimeStamp { get; set; }

        // datetime: '2018-12-06T17:44:13.667Z',
        [JsonProperty("datetime")]
        public string DateTime { get; set; }

        // lastTradeTimestamp: undefined,

        // status: 'open',
        [JsonProperty("status")]
        public string Status { get; set; }

        // symbol: 'REP/ETH',
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        // type: 'limit',
        [JsonProperty("type")]
        public string Type { get; set; }

        // side: 'buy',
        [JsonProperty("side")]
        public string Side { get; set; }

        // price: 0.052,
        [JsonProperty("price")]
        public decimal Price { get; set; }

        // amount: 0.1,
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        // cost: 0,
        [JsonProperty("cost")]
        public decimal Cost { get; set; }

        // filled: 0,
        [JsonProperty("filled")]
        public decimal Filled { get; set; }

        // remaining: 0.1,
        [JsonProperty("remaining")]
        public decimal Remaining { get; set; }

        // trades: undefined,


        [JsonProperty("info")]
        public ResponseInfo Info { get; set; }

        // fee: {
        [JsonProperty("fee")]
        public ResponseFee Fee { get; set; }

        public class ResponseFee
        {
            // cost: undefined,
            // currency: 'ETH',
            [JsonProperty("currency")]
            public string Currency { get; set; }

            // rate: undefined
        }

        public class ResponseInfo
        {
            // id: 28768684301,
            [JsonProperty("id")]
            public long Id { get; set; }

            // currencyPair: 'REP/ETH',
            [JsonProperty("currencyPair")]
            public string CurrencyPair { get; set; }

            // goodUntilTime: 0,
            [JsonProperty("goodUntilTime")]
            public long GoodUntilTime { get; set; }

            // type: 'LIMIT_BUY',
            [JsonProperty("type")]
            public string Type { get; set; }

            // orderStatus: 'OPEN',
            [JsonProperty("orderStatus")]
            public string OrderStatus { get; set; }

            // issueTime: 1544118253667,
            [JsonProperty("issueTime")]
            public long IssueTime { get; set; }

            // price: 0.052,
            [JsonProperty("price")]
            public decimal? Price { get; set; }

            // quantity: 0.1,
            [JsonProperty("quantity")]
            public decimal? Quantity { get; set; }

            // remainingQuantity: 0.1,
            [JsonProperty("remainingQuantity")]
            public decimal? RemainingQuantity { get; set; }

            // commissionByTrade: 0,
            [JsonProperty("commissionByTrade")]
            public decimal? CommissionByTrade { get; set; }

            // bonusByTrade: 0,
            [JsonProperty("bonusByTrade")]
            public decimal? BonusByTrade { get; set; }

            // bonusRate: 0,
            [JsonProperty("bonusRate")]
            public decimal? BonusRate { get; set; }

            // commissionRate: 0.0018,
            [JsonProperty("commissionRate")]
            public decimal? CommissionRate { get; set; }

            // lastModificationTime: 1544118253667
            [JsonProperty("lastModificationTime")]
            public long LastModificationTime { get; set; }
        }
    }
}
