using trade_lib;

namespace blocktrade_lib
{
    public interface IBlockTradeExchange : 
        ITradeIntegration,
        ILimitIntegrationWithResult,
        IExchangeGetOpenOrdersV2,
        ICancelOrderIntegration
    {
    }
}
