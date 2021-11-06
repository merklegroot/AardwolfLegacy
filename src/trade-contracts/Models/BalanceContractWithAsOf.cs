using System;

namespace trade_contracts.Models
{
    public class BalanceContractWithAsOf : BalanceContract
    {
        public DateTime? AsOfUtc { get; set; }
    }
}
