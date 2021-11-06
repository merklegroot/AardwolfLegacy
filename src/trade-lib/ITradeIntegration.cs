using System;
using System.Collections.Generic;
using cache_lib.Models;
using trade_model;

namespace trade_lib
{
    public interface ITradeIntegration : INamedIntegration
    {
        Guid Id { get; }

        HoldingInfo GetHoldings(CachePolicy cachePolicy);
        Dictionary<string, decimal> GetWithdrawalFees(CachePolicy cachePolicy);
        decimal? GetWithdrawalFee(string symbol, CachePolicy cachePolicy);
        List<CommodityForExchange> GetCommodities(CachePolicy cachePolicy);
        OrderBook GetOrderBook(TradingPair tradingPair, CachePolicy cachePolicy);
        List<TradingPair> GetTradingPairs(CachePolicy cachePolicy);

        List<DepositAddressWithSymbol> GetDepositAddresses(CachePolicy cachePolicy);
        DepositAddress GetDepositAddress(string symbol, CachePolicy cachePolicy);        
    }

    public interface IBuyLimitIntegration
    {
        bool BuyLimit(TradingPair tradingPair, decimal quantity, decimal price);
    }

    public interface ISellLimitIntegration
    {
        bool SellLimit(TradingPair tradingPair, decimal quantity, decimal price);
    }
}
