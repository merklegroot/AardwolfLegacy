using Newtonsoft.Json;
using System;

namespace coss_lib.Models
{
    public class CreateWithdrawalPayload
    {
        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("wallet_guid")]
        public Guid WalletGuid { get; set; }

        [JsonProperty("wallet_address")]
        public string WalletAddress { get; set; }

        [JsonProperty("tfa_token")]
        public string TfaToken { get; set; }
    }
}
