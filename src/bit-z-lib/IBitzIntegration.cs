using cache_lib.Models;
using System.Collections.Generic;
using trade_lib;
using trade_model;

namespace bit_z_lib
{
    public interface IBitzIntegration :
        ITradeIntegration,
        IBuyLimitIntegration, ISellLimitIntegration,
        ITradeHistoryIntegration,
        IExchangeWithOpenOrders,
        ICommodityIntegration,
        ITradingPairIntegration,
        ITradeGetCachedOrderBooks,
        IExchangeGetOpenOrdersV2,
        ICancelOrderIntegration
    {
        List<OpenOrder> GetOpenOrdersForTradingPair(TradingPair tradingPair, CachePolicy cachePolicy);
        void CancelAllOpenOrdersForTradingPair(TradingPair tradingPair);

        void UpdateCoinList();
    }
}
