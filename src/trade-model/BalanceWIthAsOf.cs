using System;
using System.Collections.Generic;
using System.Linq;

namespace trade_model
{
    public class BalanceWithAsOf : Balance
    {
        public DateTime? AsOfUtc { get; set; }
    }

    public static class BalanceWithAsOfExtensions
    {
        public static BalanceWithAsOf ForSymbol(this IEnumerable<BalanceWithAsOf> balances, string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }

            if (balances == null) { return null; }            

            return balances.FirstOrDefault(queryBalance => 
                string.Equals(queryBalance.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
            );
        }
    }
}
