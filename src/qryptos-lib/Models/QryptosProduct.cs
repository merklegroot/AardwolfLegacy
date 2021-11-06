using Newtonsoft.Json;

namespace qryptos_lib.Models
{
    public class QryptosProduct
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        public string product_type { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public decimal? market_ask { get; set; }
        public decimal? market_bid { get; set; }
        public long? indicator { get; set; }
        public string currency { get; set; }
        public string currency_pair_code { get; set; }
        public string symbol { get; set; }
        public decimal? btc_minimum_withdraw { get; set; }
        public decimal? fiat_minimum_withdraw { get; set; }
        public string pusher_channel { get; set; }
        public double taker_fee { get; set; }
        public double maker_fee { get; set; }
        public string low_market_bid { get; set; }
        public string high_market_ask { get; set; }
        public string volume_24h { get; set; }
        public string last_price_24h { get; set; }
        public decimal? last_traded_price { get; set; }
        public decimal? last_traded_quantity { get; set; }
        public string quoted_currency { get; set; }
        public string base_currency { get; set; }
        public bool internal_token_sale { get; set; }
        public bool disabled { get; set; }
    }
}
