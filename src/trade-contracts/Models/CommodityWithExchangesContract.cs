using System.Collections.Generic;

namespace trade_contracts
{
    public class CommodityWithExchangesContract : CommodityContract
    {
        public List<string> Exchanges { get; set; }
        public string Contract { get; set; }
    }
}
