using cache_lib.Models;
using trade_model;

namespace trade_lib
{
    public interface IBalanceIntegration
    {
        BalanceWithAsOf GetBalanceForSymbol(string symbol, CachePolicy cachePolicy);
    }
}
