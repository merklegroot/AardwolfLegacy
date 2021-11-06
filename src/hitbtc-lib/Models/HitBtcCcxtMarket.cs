using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hitbtc_lib.Models
{
    public class HitBtcCcxtMarket
    {
        public bool tierBased { get; set; }
        public bool percentage { get; set; }
        public decimal? taker { get; set; }
        public decimal? maker { get; set; }
        public Info info { get; set; }
        public string id { get; set; }
        public string symbol { get; set; }

        [JsonProperty("base")]
        public string Base { get; set; }
        public string quote { get; set; }
        public string baseId { get; set; }
        public string quoteId { get; set; }
        public bool active { get; set; }
        public decimal? lot { get; set; }
        public decimal? step { get; set; }
        public Precision precision { get; set; }
        public Limits limits { get; set; }

        public class Info
        {
            public string id { get; set; }
            public string baseCurrency { get; set; }
            public string quoteCurrency { get; set; }
            public string quantityIncrement { get; set; }
            public string tickSize { get; set; }
            public string takeLiquidityRate { get; set; }
            public string provideLiquidityRate { get; set; }
            public string feeCurrency { get; set; }
        }

        public class Precision
        {
            public decimal? price { get; set; }
            public decimal? amount { get; set; }
        }

        public class Amount
        {
            public decimal? min { get; set; }
        }

        public class Price
        {
            public decimal? min { get; set; }
        }

        public class Cost
        {
            public decimal? min { get; set; }
        }

        public class Limits
        {
            public Amount amount { get; set; }
            public Price price { get; set; }
            public Cost cost { get; set; }
        }

    }
}
