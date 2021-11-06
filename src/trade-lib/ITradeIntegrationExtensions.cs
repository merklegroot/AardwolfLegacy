using System.Linq;
using cache_lib.Models;
using trade_model;

namespace trade_lib
{
    public static class ITradeIntegrationExtensions
    {
        public static OrderBook GetOrderBook(this ITradeIntegration integration, TradingPair tradingPair, bool forceRefresh = false)
        {
            return integration.GetOrderBook(tradingPair, forceRefresh ? CachePolicy.ForceRefresh : CachePolicy.AllowCache);
        }

        public static Holding GetHolding(this ITradeIntegration integration, string symbol, CachePolicy cachePolicy)
        {
            var holdings = integration.GetHoldings(cachePolicy);
            return holdings?.Holdings?.SingleOrDefault(item => string.Equals(item.Asset, symbol));
        }
    }
}
