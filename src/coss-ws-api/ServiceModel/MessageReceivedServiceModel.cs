using Newtonsoft.Json;
using System;

namespace coss_ws_api.ServiceModel
{
    public class MessageReceivedServiceModel
    {
        [JsonProperty("timeStampUtc")]
        public DateTime TimeStampUtc { get; set; }

        [JsonProperty("contract")]
        public string Contract { get; set; }

        [JsonProperty("messageContents")]
        public string MessageContents { get; set; }
    }
}