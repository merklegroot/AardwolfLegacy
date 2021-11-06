using System.Collections.Generic;

namespace hitbtc_lib.Models
{
    public class HitBtcOrderBook
    {
        public List<HitBtcOrder> Ask { get; set; }
        public List<HitBtcOrder> Bid { get; set; }
    }
}
