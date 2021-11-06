using System.Collections.Generic;

namespace trade_contracts
{
    public class BalanceContract
    {
        public string Symbol { get; set; }

        public decimal Available { get; set; }
        public decimal InOrders { get; set; }
        public decimal Total { get; set; }

        public Dictionary<string, decimal> AdditionalBalances { get; set; }
    }
}
