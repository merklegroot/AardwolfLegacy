using System.Collections.Generic;

namespace binance_lib.Models.Canonical
{
    public class BcExchangeInfo
    {
        public string Timezone { get; set; }
        public long ServerTime { get; set; }
        public List<BcRateLimit> RateLimits { get; set; }
        public List<BcSymbol> Symbols { get; set; }
    }
}
