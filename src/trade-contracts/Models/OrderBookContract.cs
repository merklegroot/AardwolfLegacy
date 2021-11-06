using System;
using System.Collections.Generic;

namespace trade_contracts
{
    public class OrderBookContract
    {
        public DateTime? AsOf { get; set; }
        public List<OrderContract> Asks { get; set; }
        public List<OrderContract> Bids { get; set; }
    }
}
