using Newtonsoft.Json;
using System;

namespace trade_browser_lib.Models
{
    public class CossSession
    {
        public class CossSessionPayload
        {
            [JsonProperty("guid")]
            public Guid Id { get; set; }

            [JsonProperty("full_name")]
            public string FullName { get; set; }

            [JsonProperty("username")]
            public string UserName { get; set; }

            [JsonProperty("email_address")]
            public string EmailAddress { get; set; }

            [JsonProperty("kyc_level_guid")]
            public Guid? KycLevelGuid { get; set; }
        }

        public bool Successful { get; set; }
        public CossSessionPayload Payload { get; set; }
    }
}
