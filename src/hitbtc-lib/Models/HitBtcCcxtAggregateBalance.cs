using System.Collections.Generic;

namespace hitbtc_lib.Models
{
    public class HitBtcCcxtAggregateBalance
    {
        public List<HitBtcCcxtBalanceItem> TradingAccount { get; set; }
        public List<HitBtcCcxtBalanceItem> MainAccount { get; set; }
    }
}
