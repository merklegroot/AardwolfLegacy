using System.Collections.Generic;

namespace trade_model
{
    public class Balance
    {
        public string Symbol { get; set; }
        public decimal? Available { get; set; }
        public decimal? InOrders { get; set; }
        public decimal? Total { get; set; }
        public Dictionary<string, decimal> AdditionalBalanceItems { get; set; }
    }
}
