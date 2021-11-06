using cache_lib.Models;
using hitbtc_lib.Models;
using System.Collections.Generic;
using trade_lib;

namespace hitbtc_lib
{
    public interface IHitBtcIntegration : 
        ITradeIntegration, 
        IRefreshable, 
        IWithdrawableTradeIntegration,
        ITradeGetCachedOrderBooks,
        ITradingPairIntegration,
        IExchangeWithOpenOrdersForTradingPair,
        IExchangeGetOpenOrdersForTradingPairV2,
        ICancelOrderIntegration,
        ILimitIntegrationWithResult,
        ITradeHistoryIntegrationV2
    {
        List<HitBtcHealthStatusItem> GetHealth(CachePolicy cachePolicy);
        void KeepHealthFresh();
    }
}
