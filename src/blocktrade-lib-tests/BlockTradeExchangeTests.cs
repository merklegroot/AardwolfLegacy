using blocktrade_lib;
using BlocktradeExchangeLib;
using cache_lib;
using cache_lib.Models;
using config_client_lib;
using dump_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;
using trade_model;

namespace blocktrade_lib_tests
{
    [TestClass]
    public class BlockTradeExchangeTests
    {
        private BlockTradeExchange _blockTradeExchange;

        [TestInitialize]
        public void Setup()
        {
            var blockTradeClient = new BlocktradeClient();
            var configClient = new ConfigClient();
            var cacheUtil = new CacheUtil();

            var log = new Mock<ILogRepo>();
            _blockTradeExchange = new BlockTradeExchange(configClient, blockTradeClient, cacheUtil, log.Object);
        }

        [TestMethod]
        public void Blocktrade__get_commodities__allow_cache()
        {
            var commodities = _blockTradeExchange.GetCommodities(CachePolicy.AllowCache);
            commodities.Dump();
        }

        [TestMethod]
        public void Blocktrade__get_trading_pairs__force_refresh()
        {
            var pairs = _blockTradeExchange.GetTradingPairs(CachePolicy.ForceRefresh);
            pairs.Dump();
        }

        [TestMethod]
        public void Blocktrade__get_kaya_trading_pairs()
        {
            var pairs = _blockTradeExchange.GetTradingPairs(CachePolicy.ForceRefresh);
            var kayaTradingPairs = pairs.Where(item => string.Equals(item.Symbol, "KAYA", StringComparison.InvariantCultureIgnoreCase)).ToList();
            kayaTradingPairs.Dump();
        }

        [TestMethod]
        public void Blocktrade__get_kaya_eth_order_book__allow_cache()
        {
            var orderBook = _blockTradeExchange.GetOrderBook(new TradingPair("KAYA", "ETH"), CachePolicy.AllowCache);
            orderBook.Dump();
        }

        [TestMethod]
        public void Blocktrade__get_kaya_eth_order_book__force_refresh()
        {
            var orderBook = _blockTradeExchange.GetOrderBook(new TradingPair("KAYA", "ETH"), CachePolicy.ForceRefresh);
            orderBook.Dump();
        }

        [TestMethod]
        public void Blocktrade__get_balances__force_refresh()
        {
            var results = _blockTradeExchange.GetHoldings(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Blocktrade__get_balances__allow_cache()
        {
            var results = _blockTradeExchange.GetHoldings(CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Blocktrade__place_limit_ask__eth_btc()
        {
            var results = _blockTradeExchange.SellLimit("ETH", "BTC", new QuantityAndPrice
            {
                Quantity = 0.01m,
                Price = 0.03872m
            });

            results.Dump();
        }

        [TestMethod]
        public void Blocktrade__place_limit_bid__eth_btc()
        {
            var results = _blockTradeExchange.BuyLimit("ETH", "BTC", new QuantityAndPrice
            {
                Quantity = 0.01m,
                Price = 0.031m
            });

            results.Dump();
        }

        [TestMethod]
        public void Blocktrade__get_open_orders__eth_btc__force_refresh()
        {
            var results = _blockTradeExchange.GetOpenOrdersForTradingPairV2("ETH", "BTC", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Blocktrade__get_open_orders__eth_btc__allow_cache()
        {
            var results = _blockTradeExchange.GetOpenOrdersForTradingPairV2("ETH", "BTC", CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Blocktrade__get_open_orders__ltc_btc__allow_cache()
        {
            var results = _blockTradeExchange.GetOpenOrdersForTradingPairV2("LTC", "BTC", CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Blocktrade__get_open_orders()
        {
            var results = _blockTradeExchange.GetOpenOrdersV2();
            results.Dump();
        }

        //[TestMethod]
        //public void Blocktrade__get_user_activities()
        //{
        //    var results = _blockTradeExchange.GetUserActivities();
        //    results.Dump();
        //}

        //[TestMethod]
        //public void Blocktrade__get_user_activity_detail()
        //{
        //    const int ActivityId = 1599;
        //    var results = _blockTradeExchange.GetUserActivityDetail(ActivityId);
        //    results.Dump();
        //}

        [TestMethod]
        public void Blocktrade__cancel_order()
        {
            const string OrderId = "14228425";
            _blockTradeExchange.CancelOrder(OrderId);
        }

        [TestMethod]
        public void Blocktrade__cancel_open_orders_individually()
        {
            var openOrdersResponse = _blockTradeExchange.GetOpenOrdersForTradingPairV2("ETH", "BTC", CachePolicy.ForceRefresh);
            foreach(var openOrder in openOrdersResponse.OpenOrders)
            {
                _blockTradeExchange.CancelOrder(openOrder.OrderId);
            }
        }
    }
}
