using System;
using System.Collections.Generic;

namespace trade_model
{
    public class CommodityListing
    {
        public string CommoditySymbol { get; set; }
        public string CommodityName { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public string TweetText { get; set; }
        public List<string> Links { get; set; }
    }
}
