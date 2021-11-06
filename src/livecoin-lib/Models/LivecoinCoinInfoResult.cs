using System.Collections.Generic;

namespace livecoin_lib.Models
{
    public class LivecoinCoinInfoResult
    {
        public bool Success { get; set; }
        public decimal MinimalOrderBTC { get; set; }
        public List<LivecoinCoinInfoItem> Info { get; set; }
    }
}
