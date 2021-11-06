using System;

namespace trade_contracts.Messages.CryptoCompare
{
    public class UsdValueResult
    {
        public DateTime? AsOfUtc { get; set; }
        public decimal? UsdValue { get; set; }
    }
}
