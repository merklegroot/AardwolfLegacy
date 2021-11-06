using System;

namespace trade_model
{
    public class Exchange
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public bool IsRefreshable { get; set; }

        public bool IsWithdrawable { get; set; }

        public bool CanBuyMarket { get; set; }

        public bool CanSellMarket { get; set; }

        public bool HasOrderBooks { get; set; }
    }
}
