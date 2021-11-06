using cache_lib.Models;
using coss_data_lib;
using exchange_client_lib;
using log_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using task_lib;
using trade_model;
using trade_res;

namespace coss_agent_lib.Strategy
{
    public class CossAutoOpenBid : ICossAutoOpenBid
    {
        private readonly IExchangeClient _exchangeClient;
        private readonly ICossXhrOpenOrderRepo _cossXhrOpenOrderRepo;
        private readonly ICossDriver _cossDriver;
        private readonly ILogRepo _log;

        public CossAutoOpenBid(
            IExchangeClient exchangeClient,
            ICossXhrOpenOrderRepo cossXhrOpenOrderRepo,
            ICossDriver cossDriver,
            ILogRepo log)
        {
            _exchangeClient = exchangeClient;
            _cossXhrOpenOrderRepo = cossXhrOpenOrderRepo;
            _cossDriver = cossDriver;
            _log = log;
        }

        public void Execute(CachePolicy cachePolicy = CachePolicy.ForceRefresh)
        {
            var tradingPairs = GetTradingPairs();
            foreach (var tradingPair in tradingPairs)
            {
                try
                {
                    AutoOpenBidForTradingPair(tradingPair, cachePolicy);
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    Thread.Sleep(TimeSpan.FromSeconds(2.5));
                }
            }
        }

        public List<TradingPair> GetTradingPairs()
        {
            var tradingPairs = new List<TradingPair>();
            foreach (var symbol in CossAgentRes.SimpleBinanceSymbols)
            {
                tradingPairs.Add(new TradingPair(symbol, "ETH"));
                tradingPairs.Add(new TradingPair(symbol, "BTC"));
            }

            return tradingPairs;
        }

        public void AutoOpenBidForTradingPair(TradingPair tradingPair, CachePolicy cachePolicy = CachePolicy.ForceRefresh)
        {
            // TODO: If we have an open order that's not valid,
            // TODO: cancel it no matter what.

            var openOrdersTask = LongRunningTask.Run(() => _cossDriver.GetOpenOrdersForTradingPair(tradingPair.Symbol, tradingPair.BaseSymbol));        
            var binanceOrderBookTask = LongRunningTask.Run(() => _exchangeClient.GetOrderBook(ExchangeNameRes.Binance, tradingPair.Symbol, tradingPair.BaseSymbol, cachePolicy));
            var cossOrderBookTask = LongRunningTask.Run(() => _exchangeClient.GetOrderBook(ExchangeNameRes.Coss, tradingPair.Symbol, tradingPair.BaseSymbol, cachePolicy));

            var openOrders = openOrdersTask.Result;
            var binanceOrderBook = binanceOrderBookTask.Result;
            var cossOrderBook = cossOrderBookTask.Result;

            AutoOpenBidForTradingPair(tradingPair, openOrders, binanceOrderBook, cossOrderBook);
        }

        public void AutoOpenBidForTradingPair(
            TradingPair tradingPair,
            List<OpenOrderForTradingPair> openOrders,
            OrderBook binanceOrderBook,
            OrderBook cossOrderBook)
        {
            if (binanceOrderBook == null || binanceOrderBook.Bids == null || !binanceOrderBook.Bids.Any()
                || binanceOrderBook.Asks == null || !binanceOrderBook.Asks.Any()) { return; }

            if (cossOrderBook == null || cossOrderBook.Bids == null || !cossOrderBook.Bids.Any()
                || cossOrderBook.Asks == null || !cossOrderBook.Asks.Any()) { return; }

            bool doWeAlreadyHaveAValidBid = false;
            var binanceHighestBidPrice = binanceOrderBook?.BestBid()?.Price ?? null;
            var cossHighestBidPrice = cossOrderBook?.BestBid()?.Price ?? null;

            var ordersToCancel = new List<OpenOrderForTradingPair>();
            if (openOrders != null)
            {               
                foreach (var openOrder in openOrders)
                {
                    if (openOrder.OrderType == OrderType.Bid)
                    {
                        // is the bid higher than it's worth?
                        if (binanceHighestBidPrice.HasValue && binanceHighestBidPrice > openOrder.Price)
                        {
                            ordersToCancel.Add(openOrder);
                        }
                        // is it a losing bid?
                        else if (cossHighestBidPrice.HasValue && openOrder.Price < cossHighestBidPrice)
                        {
                            ordersToCancel.Add(openOrder);
                        }
                        else
                        {
                            doWeAlreadyHaveAValidBid = true;
                        }
                    }                    
                }
            }

            if (ordersToCancel.Any())
            {
                _cossDriver.NavigateToExchange(tradingPair);

                foreach (var orderToCancel in ordersToCancel)
                {                   
                    _cossDriver.CancelOrder(orderToCancel.OrderId, orderToCancel.Symbol, orderToCancel.BaseSymbol);
                }
            }

            if (doWeAlreadyHaveAValidBid) { return; }
            
            var bidToPlace = GetBidToPlace(tradingPair, openOrders, binanceOrderBook, cossOrderBook);
            if (bidToPlace == null || bidToPlace.Quantity <= 0 || bidToPlace.Price < 0)
            {
                return;
            }

            _cossDriver.CancelAllForTradingPair(tradingPair);
            _cossDriver.PlaceOrder(tradingPair, OrderType.Bid, bidToPlace);
            _cossDriver.RefreshOpenOrders(tradingPair);
        }

        public QuantityAndPrice GetBidToPlace(TradingPair tradingPair)
        {
            var openOrders = _exchangeClient.GetOpenOrders(ExchangeNameRes.Coss, CachePolicy.ForceRefresh);
            var binanceOrderBookTask = LongRunningTask.Run(() => _exchangeClient.GetOrderBook(ExchangeNameRes.Binance, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh));
            var cossOrderBookTask = LongRunningTask.Run(() => _exchangeClient.GetOrderBook(ExchangeNameRes.Coss, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh));

            var binanceOrderBook = binanceOrderBookTask.Result;
            var cossOrderBook = cossOrderBookTask.Result;

            if (binanceOrderBook == null || binanceOrderBook.Bids == null || !binanceOrderBook.Bids.Any()
                || binanceOrderBook.Asks == null || !binanceOrderBook.Asks.Any()) { return null; }
            
            if (cossOrderBook == null || cossOrderBook.Bids == null || !cossOrderBook.Bids.Any()
                || cossOrderBook.Asks == null || !cossOrderBook.Asks.Any()) { return null; }

            return GetBidToPlace(tradingPair, openOrders, binanceOrderBook, cossOrderBook);
        }

        public QuantityAndPrice GetBidToPlace(
            TradingPair tradingPair,
            List<OpenOrderForTradingPair> openOrders,
            OrderBook binanceOrderBook,
            OrderBook cossOrderBook)
        {
            var binanceBestBid = binanceOrderBook.BestBid();
            var binanceBestBidPrice = binanceBestBid.Price;
            var binanceBestAsk = binanceOrderBook.BestAsk();
            var binanceBestAskPrice = binanceBestAsk.Price;

            var cossBestBid = cossOrderBook.BestBid();
            var cossBestBidPrice = cossBestBid.Price;
            var cossBestAsk = cossOrderBook.BestAsk();
            var cossBestAskPrice = cossBestAsk.Price;

            const decimal IdealRatio = 0.9m;
            const decimal HighestAcceptableRatio = 0.96m;

            decimal currentRatio = IdealRatio;
            decimal priceToBid;
            decimal step = 0.01m;
            priceToBid = binanceBestBidPrice * currentRatio;
            while (priceToBid < cossBestBidPrice && currentRatio + step < HighestAcceptableRatio)
            {
                currentRatio += 0.01m;
                priceToBid = binanceBestBidPrice * currentRatio;
            }

            // Don't place a losing bid.
            if (priceToBid < cossBestBidPrice) { return null; }

            // auto order should have already purchased this one.
            // if this scenario occurs, something has gone wrong or someone placed an order quickly.
            if (priceToBid > cossBestAskPrice) { return null; }

            // something has gone wrong...
            if (priceToBid > binanceBestAskPrice) { return null; }

            decimal targetQuantity;
            if (string.Equals(tradingPair.BaseSymbol, "ETH", StringComparison.InvariantCultureIgnoreCase))
            {
                targetQuantity = 0.25m / priceToBid;
            }
            else if (string.Equals(tradingPair.BaseSymbol, "BTC", StringComparison.InvariantCultureIgnoreCase))
            {
                targetQuantity = 0.01m / priceToBid;
            }
            else
            {
                throw new ApplicationException($"Unexpected base symbol \"{tradingPair.BaseSymbol}\".");
            }

            return new QuantityAndPrice { Quantity = targetQuantity, Price = priceToBid };
        }
    }
}
