using System;
using dump_lib;
using fork_delta_integration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using test_shared;
using cache_lib.Models;
using trade_model;
using web_util;

namespace fork_delta_integration_tests
{
    [TestClass]
    public class ForkDeltaIntegrationTests
    {
        private ForkDeltaIntegration _integration;

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            _integration = new ForkDeltaIntegration(webUtil, IntegrationTests.DatabaseContext);
        }

        [TestMethod]
        public void Fork_delta__get_commodities()
        {
            var results = _integration.GetCommodities();
            Console.WriteLine($"Total Commodities: {results.Count}");
            results.Dump();
        }

        [TestMethod]
        public void Fork_delta__get_order_book()
        {
            var results = _integration.GetOrderBook(new TradingPair("EOS", "ETH"), CachePolicy.ForceRefresh);
            results.Dump();
        }
    }
}
