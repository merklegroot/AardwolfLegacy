using trade_lib;

namespace kucoin_lib
{
    public interface IKucoinIntegration :
        ITradeIntegration,
        IWithdrawableTradeIntegration,
        IBuyAndSellIntegration,
        IBuyLimitIntegration,
        ISellLimitIntegration,
        ITradeHistoryIntegration,
        ITradeGetCachedOrderBooks,
        // IExchangeWithOpenOrdersForTradingPair,
        IExchangeGetOpenOrdersV2,
        ICancelOrderIntegration,
        IBalanceIntegration
    {
    }
}
