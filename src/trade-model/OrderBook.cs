using System;
using System.Collections.Generic;

namespace trade_model
{
    public class OrderBook
    {
        public DateTime? AsOf { get; set; }
        public List<Order> Asks { get; set; }
        public List<Order> Bids { get; set; }
    }
}
