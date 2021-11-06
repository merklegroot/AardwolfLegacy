using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KucoinClientModelLib
{
    public class KucoinClientGetCurrenciesResponse
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("data")]
        public List<KucoinClientCurrency> Data { get; set; }
    }
}
