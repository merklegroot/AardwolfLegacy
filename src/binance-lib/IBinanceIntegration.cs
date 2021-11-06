using trade_lib;

namespace binance_lib
{
    public interface IBinanceIntegration :
        ITradeIntegration,
        ITradeHistoryIntegration,
        IBuyAndSellIntegration,
        IBuyLimitIntegration,
        ISellLimitIntegration,
        IWithdrawableTradeIntegration,
        ITradeGetCachedOrderBooks,
        IExchangeGetOpenOrdersV2,
        ICancelOrderIntegration
    {        
    }
}
