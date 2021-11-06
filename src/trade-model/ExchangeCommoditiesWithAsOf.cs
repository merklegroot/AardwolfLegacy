using System;
using System.Collections.Generic;

namespace trade_model
{
    public class ExchangeCommoditiesWithAsOf
    {
        public List<CommodityForExchange> Commodities { get; set; }
        public DateTime? AsOfUtc { get; set; }
    }
}
