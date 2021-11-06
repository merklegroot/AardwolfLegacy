using Newtonsoft.Json;

namespace KucoinClientModelLib
{
    public class KucoinClientWithdrawalHistoryItem
    {
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("address")]
        public string Address { get; set;  }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("walletTxId")]
        public string WalletTxId { get; set; }

        [JsonProperty("createAt")]
        public long CreateAt { get; set; }

        [JsonProperty("isInner")]
        public bool IsInner { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
}
