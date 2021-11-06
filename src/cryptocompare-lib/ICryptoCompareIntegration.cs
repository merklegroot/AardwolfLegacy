using cache_lib.Models;
using System;
using System.Collections.Generic;

namespace cryptocompare_lib
{
    public interface ICryptoCompareIntegration
    {
        Dictionary<string, decimal> GetPrices(string symbol, CachePolicy cachePolicy);
        decimal? GetPrice(string symbol, string baseSymbol, CachePolicy cachePolicy);
        decimal GetEthToBtcRatio(CachePolicy cachePolicy);
        decimal? GetUsdValue(string symbol, CachePolicy cachePolicy);
        (decimal? UsdValue, DateTime? AsOfUtc) GetUsdValueV2(string symbol, CachePolicy cachePolicy);
    }
}
