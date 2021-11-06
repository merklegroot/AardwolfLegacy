using dump_lib;
using client_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;
using cache_lib.Models;
using trade_model;
using web_util;
using yobit_lib;
using config_client_lib;
using yobit_lib.Client;

namespace yobit_integration_tests
{
    [TestClass]
    public class YobitIntegrationTests
    {
        private YobitIntegration _integration;

        [TestInitialize]
        public void Setup()
        {
            var configClient = new ConfigClient();
            var webUtil = new WebUtil();
            var log = new Mock<ILogRepo>();
            var yobitClient = new YobitClient(webUtil);

            _integration = new YobitIntegration(yobitClient, configClient, webUtil, log.Object);
        }

        [TestMethod]
        public void Yobit__get_trading_pairs__force_refresh()
        {
            var results = _integration.GetTradingPairs(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Yobit__get_trading_pairs__allow_cache()
        {
            var results = _integration.GetTradingPairs(CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Yobit__get_trading_pairs__only_use_cache_unless_empty()
        {
            var results = _integration.GetTradingPairs(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Yobit__get_order_book__ltc_btc__force_refresh()
        {
            var results = _integration.GetOrderBook(new TradingPair("LTC", "BTC"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Yobit__get_commodities__allow_cache()
        {
            var results = _integration.GetCommodities(CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Yobit__get_commodities__loc()
        {
            var results = _integration.GetCommodities(CachePolicy.AllowCache);
            var matches = results.Where(item => string.Equals(item.Symbol, "LOC"))
                .ToList();
            matches.Dump();
        }

        [TestMethod]
        public void Yobit__get_native_coins()
        {
            var results = _integration.GetNativeCoins();
            results.Dump();
        }

        [TestMethod]
        public void Yobit__get_native_coin()
        {

            var coins =
                @"
JNT
PAY
PLAY
SUB
COV
STK
CS
KNC
BHC
POLY".Trim().Split('\r').Select(item => item.Trim());
            foreach (var coin in coins)
            {
                _integration.GetNativeCoin(coin).Dump();
            }
        }
    }
}
