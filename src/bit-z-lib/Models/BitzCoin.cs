using Newtonsoft.Json;

namespace bit_z_lib.Models
{
    public class BitzCoin
    {
		// "id": "60",
        [JsonProperty("id")]
        public string Id { get; set; }

        // "name": "dkkt",
        [JsonProperty("name")]
        public string Name { get; set; }

        // "describe": "",
        [JsonProperty("describe")]
        public string Describe { get; set; }

        // "display": "Dkktoken",
        [JsonProperty("display")]
        public string Display { get; set; }

        // "status": "0",
        [JsonProperty("status")]
        public string Status { get; set; }

        // "out_status": "1",
        [JsonProperty("out_status")]
        public string OutStatus { get; set; }

        // "in_status": "1",
        [JsonProperty("in_status")]
        public string InStatus { get; set; }

        // "minout": "100.0000",
        [JsonProperty("minout")]
        public decimal? MinOut { get; set; }

        // "maxout": "100000",
        [JsonProperty("maxout")]
        public decimal? MaxOut { get; set; }

        // "out_limit": "50000.0000",
        [JsonProperty("out_limit")]
        public decimal? OutLimit { get; set; }

        // "rate_out": "0.000000",
        [JsonProperty("rate_out")]
        public decimal? RateOut { get; set; }

        // "mini_fee": "1",
        [JsonProperty("mini_fee")]
        public decimal? MiniFee { get; set; }

        // "confirm": "10",
        [JsonProperty("confirm")]
        public decimal? Confirm { get; set; }

        // "coin_url": "https:\/\/static.bibidev.com\/ucenter\/9b3957da15762ab04a32f9550c2ec96b.png",
        [JsonProperty("coin_url")]
        public string CoinUrl { get; set; }

        // "order_by": "1",
        [JsonProperty("order_by")]
        public string OrderBy { get; set; }

        // "channel_id": "1",
        [JsonProperty("channel_id")]
        public string ChannelId { get; set; }

        // "type": "0",
        [JsonProperty("type")]
        public string Type { get; set; }

        // "coin_tips": "0"
        [JsonProperty("coin_tips")]
        public string CoinTips { get; set; }
    }
}
