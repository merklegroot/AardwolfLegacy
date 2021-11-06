using Newtonsoft.Json;
using System.Collections.Generic;

namespace BlocktradeExchangeLib.Models
{
    public class BlocktradeMessage
    {
        [JsonProperty("message")]
        public List<string> Message { get; set; }
    }
}
