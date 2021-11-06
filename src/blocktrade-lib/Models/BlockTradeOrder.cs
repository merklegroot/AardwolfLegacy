using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blocktrade_lib.Models
{
    public class BlockTradeOrder
    {
        // "price": "0.03652",
        [JsonProperty("price")]
        public decimal Price { get; set; }

        // "amount": "0.06",
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        // "value": "0.00219120"
        [JsonProperty("value")]
        public decimal Value { get; set; }
    }
}
