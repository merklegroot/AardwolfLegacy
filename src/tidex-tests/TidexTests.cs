using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using tidex_integration_library;
using cache_lib.Models;
using trade_model;
using web_util;
using trade_lib.Cache;
using config_client_lib;

namespace tidex_tests
{
    [TestClass]
    public class TidexTests
    {
        private TidexIntegration _integration;
        private Mock<ISimpleWebCache> _cache;

        [TestInitialize]
        public void Setup()
        {
            _cache = new Mock<ISimpleWebCache>();
            var webUtil = new Mock<IWebUtil>();
            var configClient = new Mock<IConfigClient>();
            var log = new Mock<ILogRepo>();

            _integration = new TidexIntegration(webUtil.Object, configClient.Object, log.Object);
        }

        [TestMethod]
        public void Tidex_unit__get_order_book__failure_response()
        {
            int requestIndex = 0;

            _cache.Setup(mock => mock.Get(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(() =>
                {
                    try
                    {
                        if (requestIndex == 0)
                        {
                            return "{\"success\":0,\"error\":\"Requests too often\"}";
                        }

                        return "{\"snm_btc\":{\"asks\":[[0.00060767,0.9515]\", \"bids\":[[0.00060753,0.8254]}}";
                    }
                    finally
                    {
                        requestIndex++;
                    }
                });

            var results = _integration.GetOrderBook(new TradingPair("SNM", "BTC"), CachePolicy.ForceRefresh);

            results.Asks.Count.ShouldBe(1);
        }
    }
}
