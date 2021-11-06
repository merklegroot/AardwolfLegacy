using Newtonsoft.Json;
using System.Collections.Generic;

namespace currency_converter_lib.Models
{
    public class GetConversionRateResponse : Dictionary<string, GetConversionRateResponse.ResponseData>
    {
        // {"EUR_USD":{"val":1.146792}}
        public class ResponseData
        {
            [JsonProperty("val")]
            public decimal Val { get; set; }
        }
    }
}
