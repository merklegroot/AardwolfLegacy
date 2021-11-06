using cache_lib.Models;
using System.Collections.Generic;
using trade_contracts;

namespace exchange_client_lib.Models
{
    internal class TradingPairsForExchangeCache : MemCacheItem<List<TradingPairContract>>        
    {
    }
}
