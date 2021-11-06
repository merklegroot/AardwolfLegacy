using dump_lib;
using client_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using tidex_integration_library;
using cache_lib.Models;
using trade_model;
using web_util;
using config_client_lib;
using System;

namespace tide_integration_library_integration_tests
{
    [TestClass]
    public class TidexIntegrationTests
    {
        private TidexIntegration _integration;

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            var configClient = new ConfigClient();
            var log = new Mock<ILogRepo>();

            _integration = new TidexIntegration(webUtil, configClient, log.Object);
        }

        [TestMethod]
        public void Tidex__get_trading_pairs__force_refresh()
        {
            var result = _integration.GetTradingPairs(CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Tidex__get_commodities__force_refresh()
        {
            var result = _integration.GetCommodities(CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Tidex__get_order_book()
        {
            var result = _integration.GetOrderBook(new TradingPair("WAVES", "BTC"), CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Tidex__get_snm_btc_order_book()
        {
            var result = _integration.GetOrderBook(new TradingPair("SNM", "BTC"), CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Tidex__get_balance__force_refresh()
        {
            var result = _integration.GetHoldings(CachePolicy.ForceRefresh);
            result.Dump();
        }
    }
}
