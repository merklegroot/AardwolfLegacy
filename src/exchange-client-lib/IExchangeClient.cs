using cache_lib.Models;
using client_lib;
using exchange_client_lib.Models;
using System.Collections.Generic;
using trade_contracts;
using trade_contracts.Messages.Exchange;
using trade_contracts.Messages.Exchange.HitBtc;
using trade_model;

namespace exchange_client_lib
{
    public interface IExchangeClient : IServiceClient
    {
        List<DepositAddressWithSymbol> GetDepositAddresses(string exchange, CachePolicy cachePolicy);
        DepositAddress GetDepositAddress(string exchange, string symbol, CachePolicy cachePolicy);
        HistoryContainer GetExchangeHistory(string exchange, int limit, CachePolicy cachePolicy);
        List<CommodityForExchange> GetCommoditiesForExchange(string exchange, CachePolicy cachePolicy);
        DetailedExchangeCommodity GetCommoditiyForExchange(string exchange, string symbol, string nativeSymbol, CachePolicy cachePolicy);
        List<Exchange> GetExchanges();
        Exchange GetExchange(string name);

        List<string> GetCryptoCompareSymbols();

        List<TradingPair> GetTradingPairs(string exchange, CachePolicy cachePolicy);
        OrderBook GetOrderBook(string exchange, string symbol, string baseSymbol, CachePolicy cachePolicy);
        RefreshOrderBookResultContract RefreshOrderBook(string exchange, string symbol, string baseSymbol);

        Dictionary<string, decimal> GetWithdrawalFees(string exchange, CachePolicy cachePolicy);
        decimal? GetWithdrawalFee(string exchange, string symbol, CachePolicy cachePolicy);

        string GetExchangeName(string exchange);

        HoldingInfo GetBalances(string exchange, CachePolicy cachePolicy);
        Holding GetBalance(string exchange, string symbol, CachePolicy cachePolicy);
        List<BalanceWithAsOf> GetBalances(string exchange, List<string> symbols, CachePolicy cachePolicy);

        CommodityDetailsContract GetCommodityDetails(string symbol, CachePolicy cachePolicy);

        List<CommodityWithExchangesContract> GetCommodities(CachePolicy cachePolicy);

        List<string> GetExchangesForCommodity(string symbol, CachePolicyContract cachePolicy);

        List<HitBtcHealthStatusItemContract> GetHitBtcHealth(CachePolicyContract cachePolicy);

        List<OrderBookAndTradingPairContract> GetCachedOrderBooks(string exchange);

        bool BuyLimit(string exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice);

        LimitOrderResult BuyLimitV2(string exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice);

        bool SellMarket(string exchange, string symbol, string baseSymbol, decimal quantity);

        bool SellLimit(string exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice);

        LimitOrderResult SellLimitV2(string exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice);

        bool Withdraw(string exchange, string symbol, decimal quantity, DepositAddress address);

        List<OpenOrderForTradingPair> GetOpenOrders(string exchange, CachePolicy cachePolicy);

        List<OpenOrderForTradingPair> GetOpenOrders(string exchange, string symbol, string baseSymbol, CachePolicy cachePolicy);

        List<OpenOrdersForTradingPair> GetOpenOrdersV2(string exchange);

        OpenOrdersWithAsOf GetOpenOrdersForTradingPairV2(string exchange, string symbol, string baseSymbol, CachePolicy cachePolicy);

        void CancelOrder(string exchange, string orderId);

        void CancelOrder(string exchange, OpenOrder openOrder);

        HistoryContainerWithExchanges GetAggregateHistory(int? limit, CachePolicy cachePolicy);

        HistoryForTradingPairResult GetUserTradeHistoryForTradingPair(string exchange, string symbol, string baseSymbol, CachePolicy cachePolicy);
    }
}
