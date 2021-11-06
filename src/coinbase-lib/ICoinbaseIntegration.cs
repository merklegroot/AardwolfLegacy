using trade_lib;

namespace coinbase_lib
{
    public interface ICoinbaseIntegration :
        ITradeIntegration,
        ITradeHistoryIntegration,
        ITradeHistoryIntegrationV2,
        INamedIntegration
    {
    }
}
