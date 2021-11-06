using cache_lib.Models;
using System.Collections.Generic;
using trade_model;

namespace integration_workflow_lib
{
    public interface IValuationWorkflow
    {
        Dictionary<string, decimal> GetValuationDictionary(CachePolicy cachePolicy = CachePolicy.AllowCache);
        decimal? GetValue(string symbol, CachePolicy cachePolicy);
        AsOfWrapper<decimal?> GetUsdValueV2(string symbol, CachePolicy cachePolicy);
    }
}
