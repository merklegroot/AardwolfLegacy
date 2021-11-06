using Newtonsoft.Json;
using System;

namespace coss_lib.Models
{
    public class CossSession
    {
        public class CossSessionPayload
        {
            [JsonProperty("guid")]
            public Guid guid { get; set; }

            [JsonProperty("full_name")]
            public string full_name { get; set; }

            [JsonProperty("username")]
            public string username { get; set; }

            [JsonProperty("email_address")]
            public string EmailAddress { get; set; }

            [JsonProperty("kyc_level_guid")]
            public Guid? kyc_level_guid { get; set; }

            [JsonProperty("kyc_validated")]
            public bool? kyc_validated { get; set; }

            // "maker_fee_percentage": "0.001000",
            [JsonProperty("maker_fee_percentage")]
            public decimal? maker_fee_percentage { get; set; }
            
            // "taker_fee_percentage": "0.001000",
            [JsonProperty("taker_fee_percentage")]
            public decimal? taker_fee_percentage { get; set; }
        }

        public bool Successful { get; set; }
        public CossSessionPayload Payload { get; set; }
    }
}
