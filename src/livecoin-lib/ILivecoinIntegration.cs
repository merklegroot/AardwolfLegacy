using trade_lib;

namespace livecoin_lib
{
    public interface ILivecoinIntegration : 
        ITradeIntegration, 
        ITradeHistoryIntegration,
        IWithdrawableTradeIntegration,
        ITradeGetCachedOrderBooks,
        IBuyLimitIntegration,
        IExchangeWithOpenOrders,
        ICancelOrderIntegration
    {
    }
}
