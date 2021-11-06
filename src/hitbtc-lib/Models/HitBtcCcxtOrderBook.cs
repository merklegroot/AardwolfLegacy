using System.Collections.Generic;

namespace hitbtc_lib.Models
{
    public class HitBtcCcxtOrderBook
    {
        public List<List<decimal>> Bids { get; set; }
        public List<List<decimal>> Asks { get; set; }
    }
}
