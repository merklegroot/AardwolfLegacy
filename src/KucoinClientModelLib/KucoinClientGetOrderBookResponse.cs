using Newtonsoft.Json;
using System.Collections.Generic;

namespace KucoinClientModelLib
{
    public class KucoinClientGetOrderBookResponse
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("data")]
        public KucoinClientOrderBookData Data { get; set; }

        public class KucoinClientOrderBookData
        {
            [JsonProperty("sequence")]
            public string Sequence { get; set; }

            [JsonProperty("asks")]
            public List<List<decimal>> Asks { get; set; }

            [JsonProperty("bids")]
            public List<List<decimal>> Bids { get; set; }
        }
    }
}
