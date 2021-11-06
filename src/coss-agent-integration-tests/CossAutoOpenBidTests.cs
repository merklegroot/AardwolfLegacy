using cache_lib.Models;
using coss_agent_lib;
using coss_agent_lib.Strategy;
using coss_data_lib;
using dump_lib;
using client_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using task_lib;
using trade_model;
using trade_res;
using config_client_lib;
using exchange_client_lib;

namespace coss_agent_integration_tests
{
    [TestClass]
    public class CossAutoOpenBidTests
    {
        private IExchangeClient _exchangeClient;
        private Mock<ICossDriver> _cossDriver;
        private CossAutoOpenBid _cossAutoOpenBid;
        private CossXhrOpenOrderRepo _cossXhrOpenOrderRepo;

        [TestInitialize]
        public void Setup()
        {
            _exchangeClient = new ExchangeClient();
            var configClient = new ConfigClient();
            _cossDriver = new Mock<ICossDriver>();
            var log = new Mock<ILogRepo>();

            _cossXhrOpenOrderRepo = new CossXhrOpenOrderRepo(configClient);

            _cossAutoOpenBid = new CossAutoOpenBid(_exchangeClient, _cossXhrOpenOrderRepo, _cossDriver.Object, log.Object);
        }

        [TestMethod]
        public void Coss_auto_open_bid__execute()
        {
            var ordersPlaced = new List<dynamic>();

            _cossDriver.Setup(mock => mock.GetOpenOrdersForTradingPair(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((symbol, baseSymbol) =>
                {
                    var openOrders = _exchangeClient.GetOpenOrders(ExchangeNameRes.Coss, CachePolicy.AllowCache);
                    return openOrders.Where(queryOpenOrder => 
                        string.Equals(queryOpenOrder.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                        && string.Equals(queryOpenOrder.BaseSymbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();
                });

            _cossDriver.Setup(mock => mock.PlaceOrder(
                It.IsAny<TradingPair>(),
                It.IsAny<OrderType>(),
                It.IsAny<QuantityAndPrice>(),
                It.IsAny<bool>()))
                .Callback<TradingPair, OrderType, QuantityAndPrice, bool>(
                (tradingPair, orderType, quantityAndPrice, alreadyOnPage) =>
                {
                    ordersPlaced.Add(new { tradingPair, orderType, quantityAndPrice, alreadyOnPage });
                });

            _cossAutoOpenBid.AutoOpenBidForTradingPair(new TradingPair("OMG", "ETH"), CachePolicy.OnlyUseCacheUnlessEmpty);

            ordersPlaced.Dump();
        }

        [TestMethod]
        public void Coss_auto_open_bid__get_bid_to_place()
        {
            var result = _cossAutoOpenBid.GetBidToPlace(new TradingPair("SNM", "ETH"));
            result.Dump();
        }

        [TestMethod]
        public void Coss_auto_open_bid_for_knc_btc__only_use_cache_unless_empty()
        {
            var tradingPair = new TradingPair("KNC", "BTC");
            var cachePolicy = CachePolicy.OnlyUseCacheUnlessEmpty;

            var binanceOrderBook = _exchangeClient.GetOrderBook(ExchangeNameRes.Binance, tradingPair.Symbol, tradingPair.BaseSymbol, cachePolicy);
            var cossOrderBook = _exchangeClient.GetOrderBook(ExchangeNameRes.Coss, tradingPair.Symbol, tradingPair.BaseSymbol, cachePolicy);
            var openOrders = _exchangeClient.GetOpenOrders(ExchangeNameRes.Coss, CachePolicy.OnlyUseCacheUnlessEmpty);

            _cossAutoOpenBid.AutoOpenBidForTradingPair(tradingPair, openOrders, binanceOrderBook, cossOrderBook);
        }

        [TestMethod]
        public void Coss_auto_open_bid__get_bids_from_cached_order_books()
        {
            var cachePolicy = CachePolicy.OnlyUseCacheUnlessEmpty;

            var tradingPairs = _cossAutoOpenBid.GetTradingPairs();
            var bidsToPlace = new List<BidToPlaceWithTradingPair>();

            var cossCachedOrderBooksTask = LongRunningTask.Run(() => _exchangeClient.GetCachedOrderBooks(ExchangeNameRes.Coss));
            var binanceCachedOrderBooksTask = LongRunningTask.Run(() => _exchangeClient.GetCachedOrderBooks(ExchangeNameRes.Binance));

            var cossCachedOrderBooks = cossCachedOrderBooksTask.Result;
            var binanceCachedOrderBooks = binanceCachedOrderBooksTask.Result;

            foreach (var tradingPair in tradingPairs)
            {
                var binanceOrderBookTask = LongRunningTask.Run<OrderBook>(() =>
                {
                    var cacheMatch = binanceCachedOrderBooks.SingleOrDefault(item =>
                        string.Equals(item.Symbol, tradingPair.Symbol, StringComparison.InvariantCultureIgnoreCase)
                        && string.Equals(item.BaseSymbol, tradingPair.BaseSymbol, StringComparison.InvariantCultureIgnoreCase));

                    if (cacheMatch != null)
                    {
                        return new OrderBook
                        {
                            Asks = cacheMatch.Asks?.Select(queryOrder => new Order(queryOrder.Price, queryOrder.Quantity)).ToList(),
                            Bids = cacheMatch.Bids?.Select(queryOrder => new Order(queryOrder.Price, queryOrder.Quantity)).ToList(),
                            AsOf = cacheMatch.AsOf
                        };
                    }

                    return _exchangeClient.GetOrderBook(ExchangeNameRes.Binance, tradingPair.Symbol, tradingPair.BaseSymbol, cachePolicy);
                });

                var cossOrderBookTask = LongRunningTask.Run(() =>
                {
                    var cacheMatch = cossCachedOrderBooks.SingleOrDefault(item => 
                        string.Equals(item.Symbol, tradingPair.Symbol, StringComparison.InvariantCultureIgnoreCase)
                        && string.Equals(item.BaseSymbol, tradingPair.BaseSymbol, StringComparison.InvariantCultureIgnoreCase));
                    if (cacheMatch != null)
                    {
                        return new OrderBook
                        {
                            Asks = cacheMatch.Asks?.Select(queryOrder => new Order(queryOrder.Price, queryOrder.Quantity)).ToList(),
                            Bids = cacheMatch.Bids?.Select(queryOrder => new Order(queryOrder.Price, queryOrder.Quantity)).ToList(),
                            AsOf = cacheMatch.AsOf
                        };
                    }

                    return _exchangeClient.GetOrderBook(ExchangeNameRes.Coss, tradingPair.Symbol, tradingPair.BaseSymbol, cachePolicy);
                });

                OrderBook binanceOrderBook = null;
                try { binanceOrderBook =  binanceOrderBookTask.Result; } catch (Exception exception) { continue; }
                OrderBook cossOrderBook = null;
                try { cossOrderBook = cossOrderBookTask.Result; } catch (Exception exception) { continue; }

                var openOrders = _exchangeClient.GetOpenOrders(ExchangeNameRes.Coss, CachePolicy.OnlyUseCacheUnlessEmpty);

                var bidToPlace = _cossAutoOpenBid.GetBidToPlace(tradingPair, openOrders, binanceOrderBook, cossOrderBook);
                bidsToPlace.Add(new BidToPlaceWithTradingPair { Symbol = tradingPair.Symbol, BaseSymbol = tradingPair.BaseSymbol, BidToPlace = bidToPlace });
            }

            bidsToPlace.Dump();
        }

        // public void Coss_auto_open_bid__

        private class BidToPlaceWithTradingPair
        {
            public string Symbol { get; set; }
            public string BaseSymbol { get; set; }
            public QuantityAndPrice BidToPlace { get; set; }
        }
    }
}
