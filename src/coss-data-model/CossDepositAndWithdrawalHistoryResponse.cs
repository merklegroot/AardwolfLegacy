using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace coss_data_model
{
#pragma warning disable IDE1006 // Naming Styles
    public class CossDepositAndWithdrawalHistoryResponse
    {
        [JsonProperty("successful")]
        public bool Successful { get; set; }

        [JsonProperty("payload")]
        public CossDepositAndWithdrawalHistoryResponsePayload payload { get; set; }

        public class CossDepositAndWithdrawalHistoryResponsePayload
        {
            [JsonProperty("items")]
            public List<CossDepositAndWithdrawalHistoryResponseActionItem> items { get; set; }
        }
        public class CossDepositAndWithdrawalHistoryResponseActionItem
        {
            [JsonProperty("guid")]
            public Guid guid { get; set;}

            [JsonProperty("wallet_guid")]
            public Guid wallet_guid { get; set; }

            [JsonProperty("currency_guid")]
            public Guid currency_guid { get; set; }

            [JsonProperty("currency_code")]
            public string currency_code { get; set; }

            [JsonProperty("currency_name")]
            public string currency_name { get; set; }

            [JsonProperty("amount")]
            public decimal? amount { get; set; }

            [JsonProperty("created_at")]
            public DateTime? created_at { get; set; }

            [JsonProperty("updated_at")]
            public DateTime? updated_at { get; set; }

            [JsonProperty("type_code")]
            public string type_code { get; set; }

            [JsonProperty("wallet_address")]
            public string wallet_address { get; set; }

            [JsonProperty("transaction_hash")]
            public string transaction_hash { get; set; }

            [JsonProperty("action_code")]
            public string action_code { get; set; }

            [JsonProperty("action_details")]
            public string action_details { get; set; }
        }
    }
#pragma warning restore IDE1006 // Naming Styles
}
