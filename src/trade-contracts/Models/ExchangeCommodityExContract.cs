using System.Collections.Generic;

namespace trade_contracts
{
    public class ExchangeCommodityExContract : ExchangeCommodityContract
    {
        public string DepositAddress { get; set; }
        public string DepositMemo { get; set; }
        public List<string> BaseSymbols { get; set; }
    }
}
