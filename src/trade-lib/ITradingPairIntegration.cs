using cache_lib.Models;
using trade_model;

namespace trade_lib
{
    public interface ITradingPairIntegration
    {
        ExchangeTradingPairsWithAsOf GetTradingPairsWithAsOf(CachePolicy cachePolicy);
    }
}
