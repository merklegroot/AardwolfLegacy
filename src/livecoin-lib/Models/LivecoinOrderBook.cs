using System;
using System.Collections.Generic;

namespace livecoin_lib.Models
{
    public class LivecoinOrderBook
    {        
        public long TimeStamp { get; set; }
        public List<List<string>> Asks { get; set; }
        public List<List<string>> Bids { get; set; }
    }
}
