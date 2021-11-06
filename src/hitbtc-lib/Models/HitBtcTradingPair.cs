using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hitbtc_lib.Models
{
    public class HitBtcTradingPair
    {
        // {"id":"BCNBTC","baseCurrency":"BCN","quoteCurrency":"BTC",
        // "quantityIncrement":"100","tickSize":"0.0000000001",
        // "takeLiquidityRate":"0.001","provideLiquidityRate":"-0.0001",
        // "feeCurrency":"BTC"}
        public string Id { get; set; }
        public string BaseCurrency { get; set; }
        public string QuoteCurrency { get; set; }
        public decimal QuantityIncrement { get; set; }
        public decimal TickSize { get; set; }
        public decimal TakeLiquidityRate { get; set; }
        public decimal ProvideLiquidityRate { get; set; }
        public string FeeCurrency { get; set; }
    }
}
