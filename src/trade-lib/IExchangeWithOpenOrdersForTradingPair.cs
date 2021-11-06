using cache_lib.Models;
using System.Collections.Generic;
using trade_model;

namespace trade_lib
{
    public interface IExchangeWithOpenOrdersForTradingPair
    {
        List<OpenOrderForTradingPair> GetOpenOrders(string symbol, string baseSymbol, CachePolicy cachePolicy);
    }
}
