using BlocktradeExchangeLib.Models;
using System.Collections.Generic;

namespace BlocktradeExchangeLib
{
    public interface IBlocktradeClient
    {
        List<BlocktradeAsset> GetTradingAssets();
        string GetTradingAssetsRaw();

        string GetTradingPairsRaw();
        List<BlocktradeTradingPair> GetTradingPairs();

        string GetOrderBookRaw(int tradingPairId);
        BlocktradeOrderBook GetOrderBook(int tradingPairId);

        string GetUserRaw(BlocktradeApiKey apiKey);
        BlocktradeUser GetUser(BlocktradeApiKey apiKey);

        string GetUserPortfoliosRaw(BlocktradeApiKey apiKey);
        List<BlocktradePortfolio> GetUserPortfolios(BlocktradeApiKey apiKey);

        string CancelActiveRaw(BlocktradeApiKey apiKey);
        BlocktradeMessage CancelActive(BlocktradeApiKey apiKey);

        string GetUserOrdersRaw(BlocktradeApiKey apiKey, List<string> statuses = null);
        BlocktradeGetUserOrdersResponse GetUserOrders(BlocktradeApiKey apiKey, List<string> statuses = null);

        string GetUserActivitiesRaw(BlocktradeApiKey apiKey);
        string GetUserActivityDetailRaw(BlocktradeApiKey apiKey, long id);

        string CancelOrderRaw(BlocktradeApiKey apiKey, long orderId);
        BlocktradeMessage CancelOrder(BlocktradeApiKey apiKey, long orderId);

        string PlaceOrderRaw(
            BlocktradeApiKey apiKey,
            int portfolioId,
            string direction,
            string type,
            int tradingPairId,
            decimal amount,
            decimal price,
            string timeInForce,
            string stopPrice);

        BlocktradePlaceOrderResponse PlaceOrder(
            BlocktradeApiKey apiKey,
            int portfolioId,
            string direction,
            string type,
            int tradingPairId,
            decimal amount,
            decimal price,
            string timeInForce,
            string stopPrice);

        string PlaceLimitBidRaw(
            BlocktradeApiKey apiKey,
            string symbol,
            string baseSymbol,
            decimal quantity,
            decimal price);

        BlocktradePlaceOrderResponse PlaceLimitBid(
            BlocktradeApiKey apiKey,
            string symbol,
            string baseSymbol,
            decimal quantity,
            decimal price);

        string PlaceLimitAskRaw(
            BlocktradeApiKey apiKey,
            string symbol,
            string baseSymbol,
            decimal quantity,
            decimal price);

        BlocktradePlaceOrderResponse PlaceLimitAsk(
            BlocktradeApiKey apiKey,
            string symbol,
            string baseSymbol,
            decimal quantity,
            decimal price);
    }
}
