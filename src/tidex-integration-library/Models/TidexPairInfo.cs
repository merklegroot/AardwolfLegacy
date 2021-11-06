using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tidex_integration_library.Models
{
    // {"ltc_btc":{"decimal_places":8,"min_price":0.00000001,"max_price":3.0,"min_amount":0.001,"max_amount":1000000.0,"min_total":0.0001,"hidden":0,"fee":0.1}
    public class TixedPairInfo
    { 
        //"ltc_btc": {
        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }

        //	"decimal_places": 8,
        [JsonProperty("decimal_places")]
        public int DecimalPlaces { get; set; }

        //	"min_price": 0.00000001,
        [JsonProperty("min_price")]
        public decimal MinPrice { get; set; }

        //	"max_price": 3.0,
        [JsonProperty("max_price")]
        public decimal MaxPrice { get; set; }

        //	"min_amount": 0.001,
        [JsonProperty("min_amount")]
        public decimal MinAmount { get; set; }

        //	"max_amount": 1000000.0,
        [JsonProperty("max_amount")]
        public decimal MaxAmount { get; set; }

        //	"min_total": 0.0001,
        [JsonProperty("min_total")]
        public decimal MinTotal { get; set; }

        //	"hidden": 0,
        [JsonProperty("hidden")]
        public int Hidden { get; set; }

        //	"fee": 0.1
        [JsonProperty("fee")]
        public decimal Fee { get; set; }
    }
}
