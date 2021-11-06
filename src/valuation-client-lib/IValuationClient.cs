using cache_lib.Models;
using client_lib;

namespace valuation_client_lib
{
    public interface IValuationClient : IServiceClient
    {
        decimal? GetUsdValue(string symbol, CachePolicy cachePolicy);
    }
}
