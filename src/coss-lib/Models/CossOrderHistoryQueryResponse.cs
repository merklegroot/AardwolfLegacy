using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coss_model
{
    public class CossOrderHistoryQueryResponse : List<CossOrderHistoryQueryResponseElement>
    {

    }

    public class CossOrderHistoryQueryResponseElement
    {
        [JsonProperty("guid")]
        public Guid Id { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("total")]
        public decimal Total { get; set; }

        [JsonProperty("created_at")]
        public long CreatedAt { get; set; }
    }
}
