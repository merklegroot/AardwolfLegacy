using System;
using System.Collections.Generic;

namespace trade_contracts
{
    public class CommodityDetailsContract
    {
        public Guid? CanonicalId { get; set; }
        public List<CommodityContract> Recessives { get; set; }
        public string CanonicalName { get; set; }
        public string Symbol { get; set; }
        public string Website { get; set; }
        public string Telegram { get; set; }

        [Obsolete("Use ExchangesWithDetails instead")]
        public Dictionary<string, List<string>> Exchanges { get; set; }

        public List<ExchangeDetails> ExchangesWithDetails { get; set; }

        public class ExchangeDetails
        {
            public string Exchange { get; set; }
            public string Symbol { get; set; }
            public string NativeSymbol { get; set; }
            public string Name { get; set; }
            public string NativeName { get; set; }
            public Guid? CanonicalId { get; set; }

            public bool? CanDeposit { get; set; }
            public bool? CanWithdraw { get; set; }
        }
    }
}
