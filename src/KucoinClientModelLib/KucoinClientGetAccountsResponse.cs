using Newtonsoft.Json;
using System.Collections.Generic;

namespace KucoinClientModelLib
{
    public class KucoinClientGetAccountsResponse
    {
        [JsonProperty("code")]
        public long Code { get; set; }

        [JsonProperty("data")]
        public List<KucoinClientAccount> Data { get; set; }
    }
}
