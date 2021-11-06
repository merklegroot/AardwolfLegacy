//using System;
//using System.Collections.Generic;
//using System.Linq;
//using cache_lib.Models;
//using trade_model;

//namespace trade_lib
//{
//    public abstract class TradeIntegrationBase : ITradeIntegration
//    {
//        public abstract string Name { get; }

//        public abstract Guid Id { get; }

//        public virtual List<string> GetCoins() => GetCommodities().Select(item => item.Symbol).ToList();    

//        public abstract List<CommodityForExchange> GetCommodities();
//        public abstract DepositAddress GetDepositAddress(string symbol);
//        public abstract List<DepositAddress> GetDepositAddresses();
        
//        public abstract HoldingInfo GetHoldings(CachePolicy cachePolicy);
//        public abstract List<HistoricalTrade> GetUserTradeHistory();
//        public abstract OrderBook GetOrderBook(TradingPair tradingPair, CachePolicy cachePolicy);
//        public abstract List<TradingPair> GetTradingPairs();
//        public abstract decimal? GetWithdrawalFee(string symbol);
//        public abstract Dictionary<string, decimal> GetWithdrawalFees();
//        public abstract void SetDepositAddress(DepositAddress depositAddress);
//        public abstract bool Withdraw(string symbol, decimal quantity, string address);
//        public abstract bool BuyMarket(TradingPair tradingPair, decimal quantity);
//        public abstract bool SellMarket(TradingPair tradingPair, decimal quantity);
//    }
//}
