using cache_lib.Models;
using trade_model;

namespace trade_lib
{
    public interface IExchangeGetOpenOrdersForTradingPairV2
    {
        OpenOrdersWithAsOf GetOpenOrdersForTradingPairV2(string symbol, string baseSymbol, CachePolicy cachePolicy);
    }
}
