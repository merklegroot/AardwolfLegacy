using cache_lib.Models;
using idex_integration_lib.Models;
using System.Collections.Generic;
using trade_lib;
using trade_model;

namespace idex_integration_lib
{
    public interface IIdexIntegration :
        ITradeIntegration,
        IExchangeWithOpenOrders,
        ITradeHistoryIntegration
    {
        List<IdexTickerItem> GetTicker(CachePolicy cachePolicy);
        bool UseRelay { get; set; }

        IdexExtendedOrderBook GetExtendedOrderBook(TradingPair tradingPair, CachePolicy cachePolicy);
    }
}
