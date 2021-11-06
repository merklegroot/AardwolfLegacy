using Newtonsoft.Json;
using System.Collections.Generic;

namespace KucoinClientModelLib
{
    public class KucoinClientGetSymbolsResponse
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("data")]
        public List<KucoinClientSymbol> Data { get; set; }
    }
}
