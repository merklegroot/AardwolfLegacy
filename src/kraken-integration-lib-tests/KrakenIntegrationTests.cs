using cache_lib.Models;
using dump_lib;
using kraken_integration_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using trade_model;
using web_util;
using config_client_lib;
using cache_lib;
using Moq;
using log_lib;
using System.Linq;
using System.Collections.Generic;
using date_time_lib;
using System;
using kraken_integration_lib.Models;

namespace kraken_integration_lib_tests
{
    [TestClass]
    public class KrakenIntegrationTests
    {
        private KrakenIntegration _integration;

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            var configClient = new ConfigClient();
            var cacheUtil = new CacheUtil();
            var log = new Mock<ILogRepo>();

            _integration = new KrakenIntegration(webUtil, configClient, cacheUtil, log.Object);
        }

        [TestMethod]
        public void Kraken__get_trading_pairs()
        {
            var tradingPairs = _integration.GetTradingPairs();
            tradingPairs.Dump();
        }

        [TestMethod]
        public void Kraken__get_native_asset_pairs()
        {
            _integration.GetNativeAssetPairs()
                .Dump();
        }

        [TestMethod]
        public void Kraken__get_assets()
        {
            var result = _integration.GetNativeAssets();
            result.Dump();
        }

        [TestMethod]
        public void Kraken__get_order_book()
        {
            //var result = _integration.GetOrderBook(new TradingPair("XRP", "ETH"));
            //var result = _integration.GetOrderBook(new TradingPair("XBT", "EUR"));
            //var result = _integration.GetOrderBook(new TradingPair("XBT", "EOS"));
            var result = _integration.GetOrderBook(new TradingPair("EOS", "BTC"), CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Kraken__get_history__force_refresh()
        {
            var result = _integration.GetUserTradeHistory(CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Kraken__get_history__only_use_cache_unless_empty()
        {
            var results = _integration.GetUserTradeHistory(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Kraken__get_history_v2__only_use_cache_unless_empty()
        {
            var results = _integration.GetUserTradeHistoryV2(CachePolicy.OnlyUseCacheUnlessEmpty);
            // results.Dump();

            var withdrawals = results.History.Where(item => item.TradeType == TradeTypeEnum.Withdraw)
                .Select(item => new { item.Symbol, item.Quantity })
                .ToList();

            withdrawals.Dump();
        }

        [TestMethod]
        public void Kraken__get_history_v2__force_refresh()
        {
            var results = _integration.GetUserTradeHistoryV2(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Kraken__get_holdings()
        {
            var result = _integration.GetHoldings(CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Kraken__get_ledgers()
        {
            var result = _integration.GetNatveLedgerWithAsOf(CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Kraken__ledger_stuff()
        {
            var result = _integration.LedgerStuff();
            result.Dump();
        }

        [TestMethod]
        public void Kraken__ledger_individual()
        {
            var result = _integration.LedgerStuff_Individual();
            result.Dump();
        }

        [TestMethod]
        public void Kraken__even_more_ledger()
        {
            // November 03 2017 10:21:18 PM (UTC-4)
            var endTimeStamp = DateTimeUtil.GetUnixTimeStamp(new DateTime(2017, 11, 3));
            var startTimeStamp = DateTimeUtil.GetUnixTimeStamp(new DateTime(2017, 9, 1));

            var query = new Dictionary<string, object>
            {
                { "start", startTimeStamp },
                { "end", endTimeStamp },
            };

            var results = _integration.LedgerWithProps(query);
            results.Dump();
        }

        [TestMethod]
        public void Kraken__get_native_trade_history()
        {
            var endTimeStamp = DateTimeUtil.GetUnixTimeStamp(new DateTime(2017, 11, 3));
            var startTimeStamp = DateTimeUtil.GetUnixTimeStamp(new DateTime(2017, 10, 20));

            var props = new Dictionary<string, object>();
            //props["start"] = startTimeStamp;
            props["end"] = endTimeStamp;
            var results = _integration.QueryPrivate("TradesHistory", props);
            results.Dump();
        }

        [TestMethod]
        public void Kraken_z()
        {
            var endTimeStamp = DateTimeUtil.GetUnixTimeStamp(DateTime.UtcNow);
            var startTimeStamp = DateTimeUtil.GetUnixTimeStamp(new DateTime(2017, 9, 1));

            var query = new Dictionary<string, object>
            {
                { "start", startTimeStamp },
                { "end", endTimeStamp },
            };

            var results = _integration.LedgerWithProps(query);
            results.Dump();

            var keys = results.Result.Ledger.Keys.ToList();
        }

        [TestMethod]
        public void Kraken__all_ledgers()
        {
            var results = _integration.GetAllLedgers();
            results.Dump();
        }
    }
}
