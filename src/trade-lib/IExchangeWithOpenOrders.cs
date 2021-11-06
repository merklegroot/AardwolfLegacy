using System.Collections.Generic;
using cache_lib.Models;
using trade_model;

namespace trade_lib
{
    public interface IExchangeWithOpenOrders
    {
        List<OpenOrderForTradingPair> GetOpenOrders(CachePolicy cachePolicy);
    }
}
