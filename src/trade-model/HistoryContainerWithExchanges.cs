using System;
using System.Collections.Generic;

namespace trade_model
{
    public class HistoryContainerWithExchanges
    {
        public Dictionary<string, DateTime?> AsOfUtcByExchange { get; set; }
        public List<HistoricalTradeWithExchange> History { get; set; }
    }
}
