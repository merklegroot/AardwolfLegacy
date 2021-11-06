using System.Collections.Generic;
using cache_lib.Models;
using trade_model;

namespace trade_lib
{
    public interface ITradeHistoryIntegration : INamedIntegration
    {
        List<HistoricalTrade> GetUserTradeHistory(CachePolicy cachePolicy);
    }

    public interface ITradeHistoryIntegrationV2
    {
        HistoryContainer GetUserTradeHistoryV2(CachePolicy cachePolicy);
    }

    public interface ITradeHistoryForTradingPairIntegration
    {
        HistoryContainer GetUserTradeHistoryForTradingPair(string symbol, string baseSymbol, CachePolicy cachePolicy);
    }
}
