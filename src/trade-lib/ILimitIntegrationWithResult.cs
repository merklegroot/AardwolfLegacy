
using trade_model;

namespace trade_lib
{
    public interface ILimitIntegrationWithResult
    {
        LimitOrderResult BuyLimit(string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice);
        LimitOrderResult SellLimit(string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice);
    }
}
