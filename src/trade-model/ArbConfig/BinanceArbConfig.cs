using System;
using System.Collections.Generic;

namespace trade_model.ArbConfig
{
    public class BinanceArbConfig
    {
        public bool IsEnabled { get; set; }
        public string ArkSaleTarget { get; set; }
        public string TusdSaleTarget { get; set; }
        public string EthSaleTarget { get; set; }
        public string LtcSaleTarget { get; set; }
        public string WavesSaleTarget { get; set; }

        public string NeoSaleTarget
        {
            get { return GetTarget("NEO"); }
            set { SetTarget("NEO", value); }
        }

        public string BtcSaleTarget
        {
            get { return GetTarget("BTC"); }
            set { SetTarget("BTC", value); }
        }

        public Dictionary<string, string> SaleTargetDictionary { get; set; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        public string GetTarget(string symbol)
        {
            return SaleTargetDictionary.ContainsKey(symbol) ? SaleTargetDictionary[symbol] : null;
        }

        public void SetTarget(string symbol, string target)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                if (SaleTargetDictionary.ContainsKey(symbol)) { SaleTargetDictionary.Remove(symbol); }
                return;
            }

            SaleTargetDictionary[symbol] = target;
        }
    }
}
