using System;
using System.Collections.Generic;

namespace trade_model
{
    public class Holding
    {
        public string Symbol { get; set; }
        public decimal Available { get; set; }
        public decimal InOrders { get; set; }
        public decimal Total { get; set; }
        public Dictionary<string, decimal> AdditionalHoldings { get; set; }

        [Obsolete("Use Symbol instead")]
        public string Asset { get { return Symbol; } set { Symbol = value; } }
    }
}
