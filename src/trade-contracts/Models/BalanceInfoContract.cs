using System;
using System.Collections.Generic;

namespace trade_contracts
{
    public class BalanceInfoContract
    {
        public DateTime? AsOfUtc { get; set; }
        public List<BalanceContract> Balances { get; set; }
    }
}
