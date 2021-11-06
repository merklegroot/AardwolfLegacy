using trade_lib;

namespace qryptos_lib
{
    public interface IQryptosIntegration :
        ITradeIntegration,
        ITradeGetCachedOrderBooks,
        IExchangeWithOpenOrdersForTradingPair,
        ICancelOrderIntegration,
        IBuyLimitIntegration,
        ISellLimitIntegration,
        IExchangeGetOpenOrdersV2,
        IBalanceIntegration
    {        
    }
}
