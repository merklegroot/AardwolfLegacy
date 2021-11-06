using System;
using System.Collections.Generic;
using System.Linq;

namespace trade_model
{
    public class HoldingInfo
    {
        public DateTime? TimeStampUtc { get; set; }

        public List<Holding> Holdings { get; set; }

        public Holding GetHoldingForSymbol(string symbol)
        {
            if (Holdings == null) { return null; }

            // TODO: Need to handle multiple account types
            // TODO: for the same commodity.
            var match = Holdings
                .FirstOrDefault(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase));

            return match;
        }

        public decimal GetAvailableForSymbol(string symbol)
        {
            return GetHoldingForSymbol(symbol)
                ?.Available ?? 0;
        }

        public decimal GetTotalForSymbol(string symbol)
        {
            return GetHoldingForSymbol(symbol)
                ?.Total ?? 0;
        }

        public HoldingInfo Clone()
            => this != null ? (HoldingInfo)MemberwiseClone() : null;
    }
}
