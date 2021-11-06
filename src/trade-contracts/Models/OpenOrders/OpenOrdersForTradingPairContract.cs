using System;
using System.Collections.Generic;

namespace trade_contracts.Models.OpenOrders
{
    public class OpenOrdersForTradingPairContract
    {
        public string Symbol { get; set; }
        public string BaseSymbol { get; set; }
        public List<OpenOrderContract> OpenOrders { get; set; }
        public DateTime? AsOfUtc { get; set; }
    }
}
