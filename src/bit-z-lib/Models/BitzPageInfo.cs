using Newtonsoft.Json;

namespace bit_z_lib.Models
{
    public class BitzPageInfo
    {
        //	"limit": "10",
        [JsonProperty("limit")]
        public decimal? Limit { get; set; }

        //	"offest": "0",
        [JsonProperty("offest")]
        public decimal? Offest { get; set; }

        //	"current_page": "1",
        [JsonProperty("current_page")]
        public decimal? CurrentPage { get; set; }

        //	"page_size": "10",
        [JsonProperty("page_size")]
        public decimal? PageSize { get; set; }

        //	"total_count": "1",
        [JsonProperty("total_count")]
        public decimal? TotalCount { get; set; }

        //	"page_count": "1"
        [JsonProperty("PageCount")]
        public decimal? PageCount { get; set; }
    }
}
