using System;
using System.Collections.Generic;
using System.Linq;
using dump_lib;
using hitbtc_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using cache_lib.Models;
using trade_model;
using trade_node_integration;
using web_util;
using config_client_lib;
using browser_automation_client_lib;
using hitbtc_lib.Client;
using Shouldly;
using Newtonsoft.Json;
using System.IO;

namespace hitbtc_lib_tests
{
    [TestClass]
    public class HitBtcIntegrationTests
    {
        private HitBtcIntegration _hitBtc;

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            var configClient = new ConfigClient();

            var log = new Mock<ILogRepo>();
            var node = new TradeNodeUtil(configClient, webUtil, log.Object);
            var hitBtcClient = new HitBtcClient(webUtil);
            var browserClient = new BrowserAutomationClient();

            _hitBtc = new HitBtcIntegration(
                webUtil,
                configClient,
                hitBtcClient,
                node,
                browserClient,
                log.Object);
        }

        [TestMethod]
        public void Hitbtc__get_holdings__force_refresh()
        {
            var holdings = _hitBtc.GetHoldings(CachePolicy.ForceRefresh);
            holdings.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_holdings__only_use_cache_if_empty()
        {
            var holdings = _hitBtc.GetHoldings(CachePolicy.OnlyUseCacheUnlessEmpty);
            holdings.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_native_holdings__only_use_cache_if_empty()
        {
            var results = _hitBtc.GetNativeHoldings(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_ccxt_holdings()
        {
            var results = _hitBtc.GetCcxtHoldings(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_mth_btc_order_book__force_refresh()
        {
            var results = _hitBtc.GetOrderBook(new TradingPair("MTH", "BTC"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_xem_usd_order_book__force_refresh()
        {
            var results = _hitBtc.GetOrderBook(new TradingPair("XEM", "USD"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_bcpt_eth_order_book__force_refresh()
        {
            var results = _hitBtc.GetOrderBook(new TradingPair("BCPT", "ETH"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_lend_btc_order_book__force_refresh()
        {
            var results = _hitBtc.GetOrderBook(new TradingPair("LEND", "BTC"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_non_existent_order_book__force_refresh()
        {
            var results = _hitBtc.GetOrderBook(new TradingPair("TESTING123", "BTC"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_eth_usdt_order_book()
        {
            var results = _hitBtc.GetOrderBook(new TradingPair("ETH", "USDT"), CachePolicy.ForceRefresh);
            results.Dump();
        }
        
        [TestMethod]
        public void Hitbtc__get_trading_pairs__allow_cache()
        {
            var tradingPairs = _hitBtc.GetTradingPairs(CachePolicy.AllowCache);
            tradingPairs.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_trading_pairs__usd__allow_cache()
        {
            var tradingPairs = _hitBtc.GetTradingPairs(CachePolicy.AllowCache);
            var matches = tradingPairs.Where(queryTradingPair =>
                string.Equals(queryTradingPair.Symbol, "USD", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(queryTradingPair.BaseSymbol, "USD", StringComparison.InvariantCultureIgnoreCase));

            matches.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_trading_pairs__force_refresh()
        {
            var tradingPairs = _hitBtc.GetTradingPairs(CachePolicy.ForceRefresh);
            tradingPairs.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_trading_pairs__only_use_cache()
        {
            var tradingPairs = _hitBtc.GetTradingPairs(CachePolicy.OnlyUseCache);
            tradingPairs.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_trading_pairs__utnp()
        {
            var tradingPairs = _hitBtc.GetTradingPairs(CachePolicy.ForceRefresh)
                .Where(item => string.Equals(item.Symbol, "UTNP", StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            tradingPairs.Dump();
        }

        public class TestResult
        {
            public TradingPair TradingPair { get; set; }
            public bool WasSuccessful { get; set; }
            public string FailureReason { get; set; }
            public string StackTrace { get; set; }
        }

        [TestMethod]
        public void Hitbtc__get_order_book_for_all_trading_pairs()
        {
            var cachePolicy = CachePolicy.AllowCache;

            var failures = new List<TradingPair>();
            var allTradingPairs = _hitBtc.GetTradingPairs(cachePolicy);
            var tp = allTradingPairs.Where(item => item.Symbol == "BITCLAVE").FirstOrDefault();

            for (var i = 0; i < allTradingPairs.Count; i++)
            {
                var tradingPair = allTradingPairs[i];

                try
                {
                    _hitBtc.GetOrderBook(tradingPair, cachePolicy);
                }
                catch
                {
                    failures.Add(tradingPair);
                }
            }

            if (failures.Any())
            {
                Console.WriteLine("The following trading pairs failed:");
                failures.Dump();
                Assert.Fail();
            }           
        }

        [TestMethod]
        public void Hitbtc__get_cached_order_books()
        {
            var results = _hitBtc.GetCachedOrderBooks();
            results.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_order_book__snm_btc__force_refresh()
        {
            var orderBook = _hitBtc.GetOrderBook(new TradingPair("SNM", "BTC"), CachePolicy.ForceRefresh);
            orderBook.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_order_book__gno_eth__force_refresh()
        {
            var orderBook = _hitBtc.GetOrderBook(new TradingPair("GNO", "ETH"), CachePolicy.ForceRefresh);
            orderBook.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_order_book__bnb_usdt__force_refresh()
        {
            var orderBook = _hitBtc.GetOrderBook(new TradingPair("BNB", "USDT"), CachePolicy.ForceRefresh);
            orderBook.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_order_book__btc_usdt__force_refresh()
        {
            var orderBook = _hitBtc.GetOrderBook(new TradingPair("BTC", "USDT"), CachePolicy.ForceRefresh);
            orderBook.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_order_book__bch_tusd__force_refresh()
        {
            var orderBook = _hitBtc.GetOrderBook(new TradingPair("BCH", "TUSD"), CachePolicy.ForceRefresh);
            orderBook.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_order_book__nano_usdt__force_refresh()
        {
            var orderBook = _hitBtc.GetOrderBook(new TradingPair("NANO", "USDT"), CachePolicy.ForceRefresh);
            orderBook.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_order_book__snm_eth__force_refresh()
        {
            var orderBook = _hitBtc.GetOrderBook(new TradingPair("SNM", "ETH"), CachePolicy.ForceRefresh);
            orderBook.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_order_book__cat_eth__force_refresh()
        {
            var orderBook = _hitBtc.GetOrderBook(new TradingPair("CAT", "ETH"), CachePolicy.ForceRefresh);
            orderBook.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_order_book__eth_usdt__force_refresh()
        {
            var orderBook = _hitBtc.GetOrderBook(new TradingPair("ETH", "USDT"), CachePolicy.ForceRefresh);
            orderBook.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_order_book__fyn_eth__force_refresh()
        {
            var orderBook = _hitBtc.GetOrderBook(new TradingPair("FYN", "ETH"), CachePolicy.ForceRefresh);
            orderBook.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_order_book__qtum_usdt__force_refresh()
        {
            var orderBook = _hitBtc.GetOrderBook(new TradingPair("QTUM", "USDT"), CachePolicy.ForceRefresh);
            orderBook.Dump();
        }
        

        [TestMethod]
        public void Hitbtc__get_bez_deposit_address__force_refresh()
        {
            var results = _hitBtc.GetDepositAddress("BEZ", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_adhive_deposit_address()
        {
            var result = _hitBtc.GetDepositAddress("ADH", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_act_deposit_address__allow_cache()
        {
            var result = _hitBtc.GetDepositAddress("ACT", CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_commodities__force_refresh()
        {
            var results = _hitBtc.GetCommodities(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_commodity__tky()
        {
            var results = _hitBtc.GetCommodities(CachePolicy.ForceRefresh);
            var match = results.SingleOrDefault(item => string.Equals(item.Symbol, "TKY", StringComparison.InvariantCultureIgnoreCase));
            match.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_commodity__mtl()
        {
            var results = _hitBtc.GetCommodities(CachePolicy.ForceRefresh);
            var match = results.SingleOrDefault(item => string.Equals(item.Symbol, "MTL", StringComparison.InvariantCultureIgnoreCase));
            match.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_commodity__usd()
        {
            var results = _hitBtc.GetCommodities(CachePolicy.ForceRefresh);
            var match = results.SingleOrDefault(item => string.Equals(item.Symbol, "USD", StringComparison.InvariantCultureIgnoreCase));
            match.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_withdrawal_fees()
        {
            var results = _hitBtc.GetWithdrawalFees(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_health__only_use_cache_unless_empty()
        {
            var results = _hitBtc.GetHealth(CachePolicy.OnlyUseCacheUnlessEmpty);
            foreach (var result in results)
            {
                var x = result.ProcessingTimeLow;
                // Console.WriteLine(x);
            }

            results.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_health__force_refresh()
        {
            var results = _hitBtc.GetHealth(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_open_orders__coss_eth__force_refresh()
        {
            var results = _hitBtc.GetOpenOrders("COSS", "ETH", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_open_orders__coss_eth__only_use_cache_unless_empty()
        {
            var results = _hitBtc.GetOpenOrders("COSS", "ETH", CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Hitbtc__cancel_open_orders()
        {
            var openOrders = _hitBtc.GetOpenOrders(CachePolicy.ForceRefresh);
            if (!openOrders.Any()) { Assert.Inconclusive("There are no open orders to cancel."); }

            foreach (var openOrder in openOrders)
            {
                _hitBtc.CancelOrder(openOrder.OrderId);
            }

            var openOrdersAfterCancelling = _hitBtc.GetOpenOrders(CachePolicy.ForceRefresh);
            openOrdersAfterCancelling.Any().ShouldBe(false);
        }

        [TestMethod]
        public void Hitbtc__buy_limit__coss_eth()
        {
            var shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string Symbol = "COSS";
            const string BaseSymbol = "ETH";

            const decimal Price = 0.000381m;
            const decimal Quantity = 250;

            var orderResult = _hitBtc.BuyLimit(Symbol, BaseSymbol, new QuantityAndPrice
            {
                Quantity = Quantity,
                Price = Price
            });

            orderResult.Dump();
        }

        [TestMethod]
        public void Hitbtc__sell_limit__coss_eth()
        {
            var shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string Symbol = "COSS";
            const string BaseSymbol = "ETH";

            const decimal Price = 0.000551m;
            const decimal Quantity = 100;

            var orderResult = _hitBtc.SellLimit(Symbol, BaseSymbol, new QuantityAndPrice
            {
                Quantity = Quantity,
                Price = Price
            });

            orderResult.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_history__allow_cache()
        {
            var results = _hitBtc.GetUserTradeHistoryV2(CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_history__force_refresh()
        {
            var results = _hitBtc.GetUserTradeHistoryV2(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_history__only_use_cache_unless_empty()
        {
            var results = _hitBtc.GetUserTradeHistoryV2(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Hitbtc__get_history_for_taxes()
        {
            var results = _hitBtc.GetUserTradeHistoryV2(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.History.Select(item => item.TimeStampUtc)
                .Select(timeStampUtc => $"{timeStampUtc.Year}-{timeStampUtc.Month}-{timeStampUtc.Day}")
                .Distinct()
                .ToList().Dump();

            // results.Dump();
        }
    }
}
