using trade_lib;

namespace kraken_integration_lib
{
    public interface IKrakenIntegration :
        ITradeIntegration,
        ITradeHistoryIntegration,
        ITradeHistoryIntegrationV2
    {
    }
}
