using Newtonsoft.Json;
using System.Collections.Generic;

namespace bit_z_lib.Models
{
    public class BitzGetBalanceResponse
    {
        [JsonProperty("info")]
        public BitzGetBalanceResponseInfo Info { get; set; }

        public class BitzGetBalanceResponseInfo
        {
            [JsonProperty("code")]
            public int Code { get; set; }

            [JsonProperty("msg")]
            public string Message { get; set; }

            [JsonProperty("uid")]
            public string Uid
            {
                get
                {
                    const string Key = "uid";
                    return Data != null && Data.ContainsKey(Key) ? (Data[Key]?.ToString()) : null;
                }
            }

            [JsonProperty("data")]
            public Dictionary<string, object> Data { get; set; }
        }

        //public class BitzGetBalanceResponseInfoData
        //{
        //    [JsonProperty("uid")]
        //    public string Uid { get; set; }
        //}
    }
}
