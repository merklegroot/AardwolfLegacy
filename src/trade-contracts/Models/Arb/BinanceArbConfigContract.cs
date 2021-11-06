using System;
using System.Collections.Generic;

namespace trade_contracts.Models.Arb
{
    public class BinanceArbConfigContract
    {
        public bool IsEnabled { get; set; }
        public string ArkSaleTarget { get; set; }
        public string TusdSaleTarget { get; set; }
        public string EthSaleTarget { get; set; }
        public string LtcSaleTarget { get; set; }
        public string WavesSaleTarget { get; set; }
        public Dictionary<string, string> SaleTargetDictionary { get; set; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
    }
}
