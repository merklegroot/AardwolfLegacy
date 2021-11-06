using cache_lib.Models;
using trade_model;

namespace trade_lib
{
    public interface ICommodityIntegration
    {
        ExchangeCommoditiesWithAsOf GetExchangeCommoditiesWithAsOf(CachePolicy cachePolicy);
    }
}
