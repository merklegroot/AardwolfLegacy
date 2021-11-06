using Newtonsoft.Json;

namespace livecoin_lib.Models
{
    public class LivecoinHistoricTradeItem
    {
        [JsonProperty("datetime")]
        public long DateTime { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("clientorderid")]
        public long ClientOrderId { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("symbol")]
        public string symbol { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        [JsonProperty("commission")]
        public decimal Commission { get; set; }

        [JsonProperty("bonus")]
        public decimal bonus { get; set; }
    }
}
