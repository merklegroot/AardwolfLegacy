using System;
using cache_lib;
using cache_lib.Models;
using config_client_lib;
using dump_lib;
using gemini_lib;
using gemini_lib.Client;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using web_util;

namespace gemini_lib_tests
{
    [TestClass]
    public class GeminiExchangeTests
    {
        private GeminiExchange _exchange;

        [TestInitialize]
        public void Setup()
        {
            var log = new Mock<ILogRepo>();
            var webUtil = new WebUtil();
            var cacheUtil = new CacheUtil();
            var client = new GeminiClient(webUtil);
            var configClient = new ConfigClient();

            _exchange = new GeminiExchange(configClient, client, cacheUtil, log.Object);
        }

        [TestMethod]
        public void Gemini__get_commodities__force_refresh()
        {
            var results = _exchange.GetCommodities(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Gemini__get_trading_pairs__force_refresh()
        {
            var results = _exchange.GetTradingPairs(CachePolicy.ForceRefresh);
            results.Dump();
        }
    }
}
