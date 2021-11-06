using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace trade_email_lib.Models
{
    public class EmailLink
    {
        [JsonProperty("messageId")]
        public string MessageId { get; set; }

        [JsonProperty("receivedTimeStamp")]
        public DateTime ReceivedTimeStamp { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }
    }
}
