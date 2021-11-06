using trade_model;

namespace trade_lib
{
    public interface IBuyAndSellIntegration
    {
        bool BuyMarket(TradingPair tradingPair, decimal quantity);
        bool SellMarket(TradingPair tradingPair, decimal quantity);

        bool BuyLimit(TradingPair tradingPair, QuantityAndPrice quantityAndPrice);
        bool SellLimit(TradingPair tradingPair, QuantityAndPrice quantityAndPrice);
    }
}
