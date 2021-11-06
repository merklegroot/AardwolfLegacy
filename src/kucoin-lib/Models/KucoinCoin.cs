using Newtonsoft.Json;

namespace kucoin_lib.Models
{
    public class KucoinCoin
    {
        //"withdrawMinFee": 0.5,
        [JsonProperty("withdrawMinFee")]
        public decimal? WithdrawMinFee { get; set; }

        //"withdrawMinAmount": 10.0,
        [JsonProperty("withdrawMinAmount")]
        public decimal? WithdrawMinAmount { get; set; }

        //"withdrawFeeRate": 0.001,
        [JsonProperty("withdrawFeeRate")]
        public decimal? WithdrawFeeRate { get; set; }

        //"confirmationCount": 12,
        [JsonProperty("confirmationCount")]
        public decimal? ConfirmationCount { get; set; }

        //"withdrawRemark": "",
        [JsonProperty("withdrawRemark")]
        public string WithdrawRemark { get; set; }

        //"infoUrl": null,
        [JsonProperty("infoUrl")]
        public string InfoUrl { get; set; }

        //"name": "Kucoin Shares",
        [JsonProperty("name")]
        public string Name { get; set; }

        //"tradePrecision": 4,
        public int TradePrecision { get; set; }

        //"depositRemark": null,
        [JsonProperty("depositRemark")]
        public string DepositRemark { get; set; }

        //"enableWithdraw": true,
        [JsonProperty("enableWithdraw")]
        public bool EnableWithdraw { get; set; }

        //"enableDeposit": true,
        [JsonProperty("enableDeposit")]
        public bool EnableDeposit { get; set; }

        //"coin": "KCS"
        [JsonProperty("coin")]
        public string Coin { get; set; }
    }
}
