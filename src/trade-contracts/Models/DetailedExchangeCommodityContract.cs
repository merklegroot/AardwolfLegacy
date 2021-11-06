using System;
using System.Collections.Generic;

namespace trade_contracts
{
    public class DetailedExchangeCommodityContract
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

        public string Exchange { get; set; }
        public string DepositAddress { get; set; }
        public string DepositMemo { get; set; }
        public decimal? LotSize { get; set; }
        public List<string> BaseSymbols { get; set; }
    }
}
