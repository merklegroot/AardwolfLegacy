using Newtonsoft.Json;
using System.Collections.Generic;

namespace coss_api_client_lib.Models
{
    public class CossApiExchangeInfo
    {
        [JsonProperty("timezone")]
        public string TimeZone { get; set; }

        [JsonProperty("server_time")]
        public long ServerTime { get; set; }

        [JsonProperty("base_currencies")]
        public List<CossApiBaseCurrency> BaseCurrencies { get; set; }

        [JsonProperty("coins")]
        public List<CossApiCoin> Coins { get; set; }

        [JsonProperty("symbols")]
        public List<CossApiSymbol> Symbols { get; set; }

        public class CossApiBaseCurrency
        {
            [JsonProperty("currency_code")]
            public string CurrencyCode { get; set; }

            [JsonProperty("minimum_total_order")]
            public decimal MinimumTotalOrder { get; set; }
        }

        public class CossApiCoin
        {
            [JsonProperty("currency_code")]
            public string CurrencyCode { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("minimum_order_amount")]
            public decimal MinimumOrderAmount { get; set; }
        }

        public class CossApiSymbol
        {
            //"symbol": "jet-eth",
            [JsonProperty("symbol")]
            public string Symbol { get; set; }

            //"amount_limit_decimal": 8,
            [JsonProperty("amount_limit_decimal")]
            public int? AmountLimitDecimal { get; set; }

            //"price_limit_decimal": 8,
            [JsonProperty("price_limit_decimal")]
            public int? PriceLimitDecimal { get; set; }

            //"allow_trading": true
            [JsonProperty("allow_trading")]
            public bool AllowTrading { get; set; }
        }
    }
}
