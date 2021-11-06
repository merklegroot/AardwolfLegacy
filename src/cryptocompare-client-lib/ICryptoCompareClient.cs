using cache_lib.Models;
using client_lib;
using System;
using System.Collections.Generic;

namespace cryptocompare_client_lib
{
    public interface ICryptoCompareClient : IServiceClient
    {
        decimal? GetUsdValue(string symbol, CachePolicy cachePolicy);
        (decimal? UsdValue, DateTime? AsOfUtc) GetUsdValueV2(string symbol, CachePolicy cachePolicy);
        Dictionary<string, decimal> GetPrices(string symbol, CachePolicy cachePolicy);
    }
}
