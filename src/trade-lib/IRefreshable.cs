using cache_lib.Models;
using trade_model;

namespace trade_lib
{
    public interface IRefreshable
    {
        RefreshCacheResult RefreshOrderBook(TradingPair tradingPair);
    }
}
