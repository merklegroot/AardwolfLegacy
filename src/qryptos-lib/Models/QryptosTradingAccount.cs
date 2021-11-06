using Newtonsoft.Json;

namespace qryptos_lib.Models
{
    public class QryptosTradingAccount
    {
        // "id": 1759,
        [JsonProperty("id")]
        public long Id { get; set; }

        // "leverage_level": 10,
        [JsonProperty("leverage_level")]
        public int? LeverageLevel { get; set; }

        // "max_leverage_level": 10,
        [JsonProperty("max_leverage_level")]
        public int? MaxLeverageLevel { get; set; }

        // "pnl": "0.0",
        [JsonProperty("pnl")]
        public decimal? Pnl { get; set; }

        // "equity": "10000.1773",
        [JsonProperty("equity")]
        public decimal? Equity { get; set; }

        // "margin": "4.2302",
        [JsonProperty("margin")]
        public decimal? Margin { get; set; }

        // "free_margin": "9995.9471",
        [JsonProperty("free_margin")]
        public decimal? FreeMargin { get; set; }

        // "trader_id": 4807,
        [JsonProperty("trader_id")]
        public long? TraderId { get; set; }

        // "status": "active",
        [JsonProperty("status")]
        public string Status { get; set; }

        // "product_code": "CASH",
        [JsonProperty("product_code")]
        public string ProductCode { get; set; }

        // "currency_pair_code": "BTCUSD",
        [JsonProperty("currency_pair_code")]
        public string CurrencyPairCode { get; set; }

        // "position": "0.1",
        [JsonProperty("position")]
        public decimal? Position { get; set; }

        // "balance": "10000.1773",
        [JsonProperty("balance")]
        public decimal? Balance { get; set; }

        // "created_at": 1421992165,
        [JsonProperty("created_at")]
        public long? CreatedAt { get; set; }

        // "updated_at": 1457242996,
        [JsonProperty("UpdatedAt")]
        public long? UpdatedAt { get; set; }

        // "pusher_channel": "trading_account_1759",
        [JsonProperty("pusher_channel")]
        public string PusherChannel { get; set; }

        // "margin_percent": "0.1",
        [JsonProperty("margin_percent")]
        public decimal? MarginPercent { get; set; }

        // "product_id": 1,
        [JsonProperty("product_id")]
        public long? ProductId { get; set; }

        // "funding_currency": "USD"
        [JsonProperty("funding_currency")]
        public string FundingCurrency { get; set; }
    }
}
