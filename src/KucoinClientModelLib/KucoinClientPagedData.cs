using Newtonsoft.Json;
using System.Collections.Generic;

namespace KucoinClientModelLib
{
    public class KucoinClientPagedData<T>
    {
        [JsonProperty("totalNum")]
        public long TotalNumber { get; set; }

        [JsonProperty("totalPage")]
        public int TotalPage { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("currentPage")]
        public int CurrentPage { get; set; }

        [JsonProperty("items")]
        public List<T> Items { get; set; }
    }
}
