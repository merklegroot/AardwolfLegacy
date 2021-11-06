using System;
using System.Collections.Generic;

namespace trade_model
{
    public class OpenOrdersForTradingPair
    {
        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }
        public List<OpenOrder> OpenOrders { get; set; }
        public DateTime? AsOfUtc { get; set; }
    }
}
