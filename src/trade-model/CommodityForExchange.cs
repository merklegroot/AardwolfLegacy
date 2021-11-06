using System;
using System.Collections.Generic;

namespace trade_model
{
    public class CommodityForExchange
    {
        public Guid? CanonicalId { get; set; }

        public string NativeSymbol { get; set; }
        public string Symbol { get; set; }
        public string Name { get; set; }
        public string NativeName { get; set; }
        public string ContractAddress { get; set; }

        public bool? CanDeposit { get; set; }
        public bool? CanWithdraw { get; set; }
        public decimal? WithdrawalFee { get; set; }

        public decimal? MinimumTradeQuantity { get; set; }
        public decimal? MinimumTradeBaseSymbolValue { get; set; }
        public decimal? LotSize { get; set; }

        public Dictionary<string, string> CustomValues { get; set; }
    }
}
