using Newtonsoft.Json;
using parse_lib;

namespace qryptos_lib.Models
{
    public class QryptosCryptoAccount
    {
		//"id": 8129421,
        [JsonProperty("id")]
        public long Id { get; set; }

        //"balance": "0.0",
        [JsonProperty("balance")]
        public string BalanceText { get; set; }

        [JsonIgnore]
        public decimal? Balance => ParseUtil.DecimalTryParse(BalanceText);

        //"address": "0xb031570ea1b6c408181318e754e87dac6c1b7789",
        [JsonProperty("address")]
        public string Address { get; set; }

        //"currency": "ADH",
        [JsonProperty("currency")]
        public string Currency { get; set; }

        //"currency_symbol": "ADH",
        [JsonProperty("currency_symbol")]
        public string CurrencySymbol { get; set; }

        //"pusher_channel": "user_282843_account_adh",
        [JsonProperty("pusher_channel")]
        public string PusherChannel { get; set; }

        //"minimum_withdraw": null,
        [JsonProperty("minimum_withdraw")]
        public string MinimumWithdraw { get; set; }

        //"lowest_offer_interest_rate": null,
        [JsonProperty("lowest_offer_interest_rate")]
        public string LowestOfferInterestRate { get; set; }

        //"highest_offer_interest_rate": null,
        [JsonProperty("highest_offer_interest_rate")]
        public string HighestOfferInterestRate { get; set; }

        //"currency_type": "crypto"
        [JsonProperty("currency_type")]
        public string CurrencyType { get; set; }
    }
}
