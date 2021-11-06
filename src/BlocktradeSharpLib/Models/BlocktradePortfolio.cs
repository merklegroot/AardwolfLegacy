using Newtonsoft.Json;
using System.Collections.Generic;

namespace BlocktradeExchangeLib.Models
{
    public class BlocktradePortfolio
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("assets")]
        public List<BlocktradePortfolioAsset> Assets { get; set; }
    }
}
