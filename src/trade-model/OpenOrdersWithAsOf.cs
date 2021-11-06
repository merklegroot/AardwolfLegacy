using System;
using System.Collections.Generic;

namespace trade_model
{
    public class OpenOrdersWithAsOf
    {
        public List<OpenOrder> OpenOrders { get; set; }
        public DateTime? AsOfUtc { get; set; }
    }
}
