using trade_lib;
using trade_model;

namespace coss_lib
{
    public interface ICossIntegration :
        ITradeIntegration,
        IRefreshable,
        IExchangeWithOpenOrders,
        IExchangeWithOpenOrdersForTradingPair,
        ITradeGetCachedOrderBooks,
        IBuyLimitIntegration,
        ISellLimitIntegration,
        ILimitIntegrationWithResult,
        ICancelOrderIntegration,
        ITradeHistoryIntegrationV2,
        IExchangeGetOpenOrdersV2,
        IWithdrawableTradeIntegration,

        // TODO: Remove this interface
        ITradeHistoryIntegration,

        ITradeHistoryForTradingPairIntegration
    {
        // todo: move this to coss-data-lib
        void InsertResponseContainer(ResponseContainer responseContainer);

        // void InsertExchangeHistory(CossResponseAndUrlContainer<CossExchangeHistoryResponse> responseContainer);
        // void InsertDepositAndWithdrawalHistory(CossResponseContainer<CossDepositAndWithdrawalHistoryResponse> responseContainer);
    }
}
