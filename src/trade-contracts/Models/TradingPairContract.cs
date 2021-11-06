using System;
using System.Collections.Generic;

namespace trade_contracts
{
    public class TradingPairContract
    {
        public Guid? CanonicalCommodityId { get; set; }

        public string Symbol { get; set; }

        public string NativeSymbol { get; set; }

        public string CommodityName { get; set; }

        public string NativeCommodityName { get; set; }

        public Dictionary<string, string> CustomCommodityValues { get; set; }

        public Guid? CanonicalBaseCommodityId { get; set; }

        public string BaseSymbol { get; set; }

        public string NativeBaseSymbol { get; set; }

        public string BaseCommodityName { get; set; }

        public Dictionary<string, string> CustomBaseCommodityValues { get; set; }

        public string NativeBaseCommodityName { get; set; }

        public decimal? LotSize { get; set; }

        public decimal? PriceTick { get; set; }

        public decimal? MinimumTradeBaseSymbolValue { get; set; }

        public decimal? MinimumTradeQuantity { get; set; }

        public TradingPairContract() { }
        public TradingPairContract(string symbol, string baseSymbol) { Symbol = symbol; BaseSymbol = baseSymbol; }

        public override string ToString()
        {
            var effectiveSymbol = !string.IsNullOrWhiteSpace(Symbol)
                ? Symbol.Trim().ToUpper()
                : "[empty]";
            var effectiveBaseSymbol = !string.IsNullOrWhiteSpace(BaseSymbol)
                ? BaseSymbol.Trim().ToUpper()
                : "[empty]";

            return $"{effectiveSymbol}-{effectiveBaseSymbol}";
        }

        public override bool Equals(object comp)
        {
            if (this == null && comp == null) { return true; }

            if (this == null) { return false; }
            if (comp == null) { return false; }

            if (comp is TradingPairContract compTp)
            {
                bool isCommodityEqual;

                if (CanonicalCommodityId.HasValue && CanonicalCommodityId.Value != default(Guid)
                    && compTp.CanonicalCommodityId.HasValue && compTp.CanonicalCommodityId.Value != default(Guid))
                {
                    isCommodityEqual = CanonicalCommodityId.Value == compTp.CanonicalCommodityId.Value;
                }
                else
                {
                    isCommodityEqual = string.Equals((compTp.Symbol ?? string.Empty).Trim(), (Symbol ?? string.Empty).Trim(), StringComparison.InvariantCultureIgnoreCase);
                }

                bool isBaseCommodityEqual;

                if (CanonicalBaseCommodityId.HasValue && CanonicalBaseCommodityId.Value != default(Guid)
                    && compTp.CanonicalBaseCommodityId.HasValue && compTp.CanonicalBaseCommodityId.Value != default(Guid))
                {
                    isBaseCommodityEqual = CanonicalBaseCommodityId == compTp.CanonicalBaseCommodityId;
                }
                else
                {
                    isBaseCommodityEqual = string.Equals((compTp.BaseSymbol ?? string.Empty).Trim(), (BaseSymbol ?? string.Empty).Trim(), StringComparison.InvariantCultureIgnoreCase);
                }

                return isCommodityEqual && isBaseCommodityEqual;
            }

            return GetHashCode().Equals(comp.GetHashCode());
        }

        public override int GetHashCode()
        {
            if (this == null) { return ((string)null).GetHashCode(); }
            var effectiveSymbolName = ((!string.IsNullOrWhiteSpace(CommodityName) ? CommodityName : Symbol) ?? string.Empty)
                .Trim().Replace(" ", string.Empty);

            var effectiveBaseSymbolName = ((!string.IsNullOrWhiteSpace(BaseCommodityName) ? BaseCommodityName : BaseSymbol) ?? string.Empty)
                .Trim().Replace(" ", string.Empty);

            // hacky...
            var effectiveString = this == null
                ? null
                : $"{effectiveSymbolName ?? string.Empty}_{effectiveBaseSymbolName ?? string.Empty}";

            return effectiveString.GetHashCode();
        }
    }
}
