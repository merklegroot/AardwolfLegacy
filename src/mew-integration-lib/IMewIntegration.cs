using trade_lib;

namespace mew_integration_lib
{
    public interface IMewIntegration :
        ITradeIntegration,
        ITradeHistoryIntegration,
        IWithdrawableTradeIntegration
    {
    }
}
