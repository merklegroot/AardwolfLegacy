using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hitbtc_lib.Models
{
    public class HitBtcSymbol
    {
		//"id": "BCNBTC",
        [JsonProperty("id")]
        public string Id { get; set; }

        //"baseCurrency": "BCN",
        [JsonProperty("baseCurrency")]
        public string BaseCurrency { get; set; }

        //"quoteCurrency": "BTC",
        [JsonProperty("quoteCurrency")]
        public string QuoteCurrency { get; set; }

        //"quantityIncrement": "100",
        [JsonProperty("quantityIncrement")]
        public decimal? QuantityIncrement { get; set; }

        //"tickSize": "0.0000000001",
        [JsonProperty("tickSize")]
        public decimal? TickSize { get; set; }

        //"takeLiquidityRate": "0.001",
        [JsonProperty("takeLiquidityRate")]
        public decimal? TakeLiquidityRate { get; set; }

        //"provideLiquidityRate": "-0.0001",
        [JsonProperty("provideLiquidityRate")]
        public decimal? ProvideLiquidityRate { get; set; }

        //"feeCurrency": "BTC"
        [JsonProperty("feeCurrency")]
        public string FeeCurrency { get; set; }
    }
}
