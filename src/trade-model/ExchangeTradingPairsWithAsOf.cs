using System;
using System.Collections.Generic;

namespace trade_model
{
    public class ExchangeTradingPairsWithAsOf
    {
        public List<TradingPair> TradingPairs { get; set; }
        public DateTime? AsOfUtc { get; set; }
    }
}
