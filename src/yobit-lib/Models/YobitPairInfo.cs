using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace yobit_lib.Models
{
    // // {"server_time":1524586042,"pairs":{"ltc_btc":
    // {"decimal_places":8,"min_price":0.00000001,"max_price":10000,
    // "min_amount":0.0001,"min_total":0.0001,"hidden":0,
    // "fee":0.2,"fee_buyer":0.2,"fee_seller":0.2}
    public class YobitPairInfo
    {
        [JsonProperty("decimal_places")]
        public int DecimalPlaces { get; set; }

        [JsonProperty("min_price")]
        public decimal MinPrice { get; set; }

        [JsonProperty("max_price")]
        public decimal MaxPrice { get; set; }

        [JsonProperty("min_amount")]
        public decimal MinAmount { get; set; }

        [JsonProperty("min_total")]
        public decimal MinTotal { get; set; }

        [JsonProperty("hidden")]
        public int Hidden { get; set; }

        [JsonProperty("fee")]
        public decimal Fee { get; set; }

        [JsonProperty("fee_buyer")]
        public decimal FeeBuyer { get; set; }

        [JsonProperty("fee_seller")]
        public decimal FeeSeller { get; set; }
    }
}
