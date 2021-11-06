using binance_lib;
using bit_z_lib;
using bitz_agent_lib.App;
using bitz_data_lib;
using cache_lib;
using cache_lib.Models;
using config_client_lib;
using log_lib;
using log_lib.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using trade_node_integration;
using web_util;

namespace bitz_agent_lib_tests
{
    [TestClass]
    public class BitzAgentDriverTests
    {
        private BitzAgentDriver _driver;

        [TestInitialize]
        public void Setup()
        {
            var configClient = new ConfigClient();
            var webUtil = new WebUtil();
            var log = new Mock<ILogRepo>();
            log.Setup(mock => mock.Info(It.IsAny<string>(), It.IsAny<EventType>(), It.IsAny<Guid?>()))
                .Callback(new Action<string, EventType, Guid?>((message, eventType, correlationId) =>
                {
                    Console.WriteLine(message);
                    Console.WriteLine();
                }));

            log.Setup(mock => mock.Error(It.IsAny<Exception>(), It.IsAny<EventType>(), It.IsAny<Guid?>()))
            .Callback(new Action<Exception, EventType, Guid?>((exception, eventType, correlationId) =>
            {
                Console.WriteLine(exception);
                Console.WriteLine();
            }));

            var nodeUtil = new TradeNodeUtil(configClient, webUtil, log.Object);
            var cacheUtil = new CacheUtil();

            var bitzFundsRepo = new BitzFundsRepo(configClient);
            var bitzClient = new BitzClient();
            var bitz = new BitzIntegration(bitzClient, configClient, nodeUtil, webUtil, bitzFundsRepo, configClient, cacheUtil, log.Object);
            var binance = new BinanceIntegration(webUtil, configClient, nodeUtil, log.Object);

            _driver = new BitzAgentDriver(bitz, binance, log.Object);
            _driver.OverrideCachePolicy = CachePolicy.OnlyUseCacheUnlessEmpty;
        }

        [TestMethod]
        public void Bitz_agent__auto_open_order()
        {
            _driver.OverrideCachePolicy = CachePolicy.ForceRefresh;
            _driver.AutoOpenOrder();
        }

        [TestMethod]
        public void Bitz_agent__auto_open_order__omisego()
        {
            _driver.OverrideCachePolicy = CachePolicy.ForceRefresh;
            _driver.AutoOpenOrder(new List<string> { "OMG" });
        }
    }
}
