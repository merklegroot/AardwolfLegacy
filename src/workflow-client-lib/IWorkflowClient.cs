using cache_lib.Models;
using client_lib;
using System;
using System.Collections.Generic;
using trade_model;

namespace workflow_client_lib
{
    public interface IWorkflowClient : IServiceClient
    {
        ArbitrageResult GetArb(string exchangeA, string exchangeB, string symbol, CachePolicy cachePolicy);
        decimal? GetUsdValue(string symbol, CachePolicy cachePolicy);

        (decimal? UsdValue, DateTime? AsOfUtc) GetUsdValueV2(string symbol, CachePolicy cachePolicy);

        Dictionary<string, decimal> GetValuationDictionary(CachePolicy cachePolicy);
    }
}
