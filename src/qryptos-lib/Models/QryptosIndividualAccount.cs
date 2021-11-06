using Newtonsoft.Json;

namespace qryptos_lib.Models
{
    public class QryptosIndividualAccount
    {
        //"id": 1433790,
        [JsonProperty("id")]
        public long Id { get; set; }

        //"currency": "QASH",
        [JsonProperty("currency")]
        public string Currency { get; set; }

        //"balance": 198.033334,
        [JsonProperty("balance")]
        public decimal? Balance { get; set; }

        //"free_balance": 98.03319052791024,
        [JsonProperty("free_balance")]
        public decimal? FreeBalance { get; set; }

        //"pnl": 0,
        [JsonProperty("pnl")]
        public decimal? Pnl { get; set; }

        //"margin": 0,
        [JsonProperty("margin")]
        public decimal? Margin { get; set; }

        //"orders_margin": 100.00014347208976,
        [JsonProperty("orders_margin")]
        public decimal? OrdersMargin { get; set; }

        //"free_margin": 198.033334,
        [JsonProperty("free_margin")]
        public decimal? FreeMargin { get; set; }

        //"equity": 198.033334,
        [JsonProperty("equity")]
        public decimal? Equity { get; set; }

        //"user_id": 282843,
        [JsonProperty("user_id")]
        public decimal? UserId { get; set; }

        //"ico_locking_trackers": []
    }
}
