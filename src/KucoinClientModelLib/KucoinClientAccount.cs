using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KucoinClientModelLib
{
    public class KucoinClientAccount
    {
        //"id": "5c6a4f8099a1d819395f110c",
        [JsonProperty("id")]
        public string Id { get; set; }

        //"balance": "0.0335835",
        [JsonProperty("balance")]
        public decimal Balance { get; set; }

        //"available": "0.0335835",
        [JsonProperty("available")]
        public decimal Available { get; set; }

        //"holds": "0",
        [JsonProperty("holds")]
        public decimal Holds { get; set; }

        //"currency": "BTC",
        [JsonProperty("currency")]
        public string Currency { get; set; }

        //"type": "trade"
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
