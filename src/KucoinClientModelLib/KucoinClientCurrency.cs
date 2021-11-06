using Newtonsoft.Json;

namespace KucoinClientModelLib
{
    public class KucoinClientCurrency
    {
        // "withdrawalMinFee": "10",
        [JsonProperty("withdrawalMinFee")]
        public decimal? WithdrawalMinFee { get; set; }

        // "precision": 8,
        [JsonProperty("precision")]
        public decimal? Precision { get; set; }

        // "name": "CSP",
        [JsonProperty("name")]
        public string Name { get; set; }

        // "fullName": "Caspian",
        [JsonProperty("fullName")]
        public string FullName { get; set; }

        // "currency": "CSP",
        [JsonProperty("currency")]
        public string Currency { get; set; }

        // "withdrawalMinSize": "20",
        [JsonProperty("withdrawalMinSize")]
        public decimal? WithdrawalMinSize { get; set; }

        // "isWithdrawEnabled": false,
        [JsonProperty("isWithdrawEnabled")]
        public bool IsWithdrawEnabled { get; set; }

        // "isDepositEnabled": false
        [JsonProperty("isDepositEnabled")]
        public bool IsDepositEnabled { get; set; }
    }
}
