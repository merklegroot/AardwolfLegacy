using Newtonsoft.Json;

namespace binance_lib.Models
{
    public class BinanceCcxtDepositAddress
    {
        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("info")]
        public BinanceCcxtDepositAddressInfo Info { get; set; }

        public class BinanceCcxtDepositAddressInfo
        {
            [JsonProperty("address")]
            public string Address { get; set; }

            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("addressTag")]
            public string AddressTag { get; set; }

            [JsonProperty("asset")]
            public string Asset { get; set; }
        }
    }
}
