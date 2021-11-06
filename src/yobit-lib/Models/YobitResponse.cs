using Newtonsoft.Json;

namespace yobit_lib.Models
{
    // {"server_time":1524586042,"pairs":{"ltc_btc":{"decimal_places":8,"min_price":0.00000001,"max_price":10000,"min_amount":0.0001,"min_total":0.0001,"hidden":0,"fee":0.2,"fee_buyer":0.2,"fee_seller":0.2},"nmc_btc":
    public class YobitResponse
    {
        [JsonProperty("server_time")]
        public long ServerTime { get; set; }
    }
}
