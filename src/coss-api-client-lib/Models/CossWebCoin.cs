using Newtonsoft.Json;

namespace coss_api_client_lib.Models
{
    public class CossWebCoin
    {
        //"currency_code": "XEM",
        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }

        //"name": "Nem",
        [JsonProperty("name")]
        public string Name { get; set; }

        //"buy_limit": 0.0,
        [JsonProperty("buy_limit")]
        public decimal BuyLimit { get; set; }

        //"sell_limit": 0.0,
        [JsonProperty("sell_limit")]
        public decimal SellLimit { get; set; }

        //"usdt": 0.0,
        [JsonProperty("usdt")]
        public decimal Usdt { get; set; }

        //"transaction_time_limit": 5,
        [JsonProperty("transaction_time_limit")]
        public decimal TransactionTimeLimit { get; set; }

        //"status": "trade",
        [JsonProperty("status")]
        public string Status { get; set; }

        //"withdrawn_fee": "13",
        [JsonProperty("withdrawn_fee")]
        public decimal? WithdrawnFee { get; set; }

        //"minimum_withdrawn_amount": "26",
        [JsonProperty("minimum_withdrawn_amount")]
        public decimal MinimumWithdrawnAmount { get; set; }

        //"minimum_deposit_amount": "13",
        [JsonProperty("minimum_deposit_amount")]
        public decimal MinimumDepositAmount { get; set; }

        //"minimum_order_amount": "0.00000001",
        [JsonProperty("minimum_order_amount")]
        public decimal MinimumOrderAmount { get; set; }

        //"decimal_format": "",
        [JsonProperty("decimal_format")]
        public string DecimalFormat { get; set; }

        //"token_type": "nem",
        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        //"buy_at": 0.0,
        [JsonProperty("buy_at")]
        public decimal BuyAt { get; set; }

        //"sell_at": 0.0,
        [JsonProperty("sell_at")]
        public decimal SellAt { get; set; }

        //"min_rate": 0.0,
        [JsonProperty("min_rate")]
        public decimal MinRate { get; set; }

        //"max_rate": 0.0,
        [JsonProperty("max_rate")]
        public decimal MaxRate { get; set; }

        //"allow_withdrawn": true,
        [JsonProperty("allow_withdrawn")]
        public bool AllowWithdrawn { get; set; }

        //"allow_deposit": true,
        [JsonProperty("allow_deposit")]
        public bool AllowDeposit { get; set; }

        //"explorer_website_mainnet_link": "http://explorer.nemchina.com",
        [JsonProperty("explorer_website_mainnet_link")]
        public string ExplorerWebsiteMainnetLink { get; set; }

        //"explorer_website_testnet_link": "http://bob.nem.ninja:8765",
        [JsonProperty("explorer_website_testnet_link")]
        public string ExplorerWebsiteTestnetLink { get; set; }

        //"deposit_block_confirmation": "1",
        [JsonProperty("deposit_block_confirmation")]
        public decimal? DepositBlockConfirmation { get; set; }

        //"withdraw_block_confirmation": "1",
        [JsonProperty("withdraw_block_confirmation")]
        public decimal? WithdrawBlockConfirmation { get; set; }

        //"icon_url": "https://s2.coinmarketcap.com/static/img/coins/32x32/873.png",
        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }

        //"is_fiat": false,
        [JsonProperty("is_fiat")]
        public bool IsFiat { get; set; }

        //"allow_sell": true,
        [JsonProperty("allow_sell")]
        public bool AllowSell { get; set; }

        //"allow_buy": true
        [JsonProperty("allow_buy")]
        public bool AllowBuy { get; set; }
    }
}
