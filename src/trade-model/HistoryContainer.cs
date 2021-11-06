using System;
using System.Collections.Generic;

namespace trade_model
{
    public class HistoryContainer
    {
        public DateTime? AsOfUtc { get; set; }
        public List<HistoricalTrade> History { get; set; }        
    }
}
