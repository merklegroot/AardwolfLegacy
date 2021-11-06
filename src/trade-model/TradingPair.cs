using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace trade_model
{
    public class TradingPair
    {
        [JsonProperty("canonicalCommodityId")]
        public Guid? CanonicalCommodityId { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("nativeSymbol")]
        public string NativeSymbol { get; set; }

        [JsonProperty("commodityName")]
        public string CommodityName { get; set; }

        [JsonProperty("nativeCommodityName")]
        public string NativeCommodityName { get; set; }

        [JsonProperty("customCommodityValues")]
        public Dictionary<string, string> CustomCommodityValues { get; set; }

        [JsonProperty("canonicalBaseCommodityId")]
        public Guid? CanonicalBaseCommodityId { get; set; }

        [JsonProperty("baseSymbol")]
        public string BaseSymbol { get; set; }

        [JsonProperty("nativeBaseSymbol")]
        public string NativeBaseSymbol { get; set; }

        [JsonProperty("baseCommodityName")]
        public string BaseCommodityName { get; set; }

        [JsonProperty("customBaseCommodityValues")]
        public Dictionary<string, string> CustomBaseCommodityValues { get; set; }

        [JsonProperty("nativeBaseCommodityName")]
        public string NativeBaseCommodityName { get; set; }

        [JsonProperty("lotSize")]
        public decimal? LotSize { get; set; }

        [JsonProperty("priceTick")]
        public decimal? PriceTick { get; set; }

        [JsonProperty("minimumTradeQuantity")]
        public decimal? MinimumTradeQuantity { get; set; }

        [JsonProperty("minimumTradeBaseSymbolValue")]
        public decimal? MinimumTradeBaseSymbolValue { get; set; }

        public TradingPair() { }
        public TradingPair(string symbol, string baseSymbol) { Symbol = symbol; BaseSymbol = baseSymbol; }

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

            if (comp is TradingPair compTp)
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
