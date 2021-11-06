using System;
using System.Collections.Generic;

namespace qryptos_lib.Models
{
    public class QryptosOrderBook
    {
        public List<List<decimal>> buy_price_levels { get; set; }
        public List<List<decimal>> sell_price_levels { get; set; }

        public DateTime? AsOf { get; set; }
    }
}
