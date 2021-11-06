using System;
using cache_lib;
using cache_lib.Models;
using config_client_lib;
using dump_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using oex_lib;
using oex_lib.Client;
using trade_model;
using web_util;

namespace oex_tests
{
    [TestClass]
    public class OexExchangeTests
    {
        private OexExchange _oexExchange;

        [TestInitialize]
        public void Setup()
        {
            var log = new Mock<ILogRepo>();
            var configClient = new ConfigClient();
            var webUtil = new WebUtil();
            var oexClient = new OexClient(webUtil);
            var cacheUtil = new CacheUtil();
            _oexExchange = new OexExchange(configClient, oexClient, cacheUtil, log.Object);
        }

        [TestMethod]
        public void Oex__get_trading_pairs__force_refresh()
        {
            var results = _oexExchange.GetTradingPairs(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Oex__get_order_book__pgt_btc__force_refresh()
        {
            var results = _oexExchange.GetOrderBook(new TradingPair("PGT", "BTC"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Oex__get_commodities__force_refresh()
        {
            var results = _oexExchange.GetCommodities(CachePolicy.ForceRefresh);
            results.Dump();
        }
    }
}
