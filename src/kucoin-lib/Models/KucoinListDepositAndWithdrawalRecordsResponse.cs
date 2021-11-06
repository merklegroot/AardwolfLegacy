using Newtonsoft.Json;
using System.Collections.Generic;

namespace kucoin_lib.Models
{
    public class KucoinListDepositAndWithdrawalRecordsResponse
    {
        // "success": true,
        [JsonProperty("success")]
        public bool Success { get; set; }

        // "code": "OK",
        [JsonProperty("code")]
        public string Code { get; set; }

        // "msg": "Operation succeeded.",
        [JsonProperty("msg")]
        public string Msg { get; set; }

        // "timestamp": 12345,
        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        // "data": {
        [JsonProperty("data")]
        public KucoinData Data { get; set; }

        public class KucoinData
        {
            // "total": 6,
            [JsonProperty("total")]
            public long Total { get; set; }

            // "firstPage": true,
            [JsonProperty("firstPage")]
            public bool FirstPage { get; set; }

            // "lastPage": false,
            [JsonProperty("lastPage")]
            public bool LastPage { get; set; }

            // "datas": [{
            [JsonProperty("datas")]
            public List<KucoinDataItem> Datas { get; set; }

            public class KucoinDataItem
            {
                // "coinType": "SOMECOIN",
                [JsonProperty("coinType")]
                public string CoinType { get; set; }

                // "createdAt": 1111,
                [JsonProperty("createdAt")]
                public long CreatedAt { get; set; }

                // "amount": 999.999,
                [JsonProperty("amount")]
                public decimal Amount { get; set; }

                // "address": "0x1111111",
                [JsonProperty("address")]
                public string Address { get; set; }

                // "fee": 1,
                [JsonProperty("fee")]
                public long Fee { get; set; }

                // "outerWalletTxid": "0x1111111",
                [JsonProperty("outerWalletTxid")]
                public string OuterWalletTxid { get; set; }

                // "remark": null,
                [JsonProperty("remark")]
                public string Remark { get; set; }

                // "oid": "111111",
                [JsonProperty("oid")]
                public string Oid { get; set; }

                // "confirmation": 0,
                [JsonProperty("confirmation")]
                public long Confirmation { get; set; }

                // "type": "WITHDRAW",
                [JsonProperty("type")]
                public string Type { get; set; }

                // "status": "SUCCESS",
                [JsonProperty("status")]
                public string Status { get; set; }

                // "updatedAt": 1111111
                [JsonProperty("updatedAt")]
                public long UpdatedAt { get; set; }
            }
        }
    }
}
