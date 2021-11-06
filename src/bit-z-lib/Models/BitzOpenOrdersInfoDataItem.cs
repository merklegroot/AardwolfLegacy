using Newtonsoft.Json;

namespace bit_z_lib.Models
{
    public class BitzOpenOrdersInfoDataItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("uid")]
        public string Uid { get; set; }
        
        [JsonProperty("price")]
        public decimal? Price { get; set; }
        
        [JsonProperty("number")]
        public decimal? Number { get; set; }
        
        [JsonProperty("total")]
        public decimal? Total { get; set; }
        
        [JsonProperty("numberOver")]
        public decimal? NumberOver { get; set; }
        
        [JsonProperty("numberDeal")]
        public decimal? NumberDeal { get; set; }
        
        [JsonProperty("flag")]
        public string Flag { get; set; }
        
        [JsonProperty("status")]
        public string Status { get; set; }
        
        [JsonProperty("isNew")]
        public string IsNew { get; set; }
        
        [JsonProperty("coinFrom")]
        public string CoinFrom { get; set; }
        
        [JsonProperty("coinTo")]
        public string CoinTo { get; set; }
        
        [JsonProperty("created")]
        public decimal? Created { get; set; }
    }
}
