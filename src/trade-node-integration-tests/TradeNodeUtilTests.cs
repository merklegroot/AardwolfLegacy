using dump_lib;
using client_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using trade_node_integration;
using web_util;
using config_client_lib;

namespace trade_node_integration_tests
{
    [TestClass]
    public class TradeNodeUtilTests
    {
        private TradeNodeUtil _node;

        [TestInitialize]
        public void Setup()
        {
            var configClient = new Mock<IConfigClient>();
            configClient.Setup(mock => mock.GetConnectionString())
                .Returns("mongodb://tradeAdmin:Trade5273!@192.168.1.26:27017");

            configClient.Setup(mock => mock.GetCcxtUrl())
                .Returns("http://iridium:3010/");

            var webUtil = new WebUtil();
            var log = new Mock<ILogRepo>();


            _node = new TradeNodeUtil(configClient.Object, webUtil, log.Object);
        }

        [TestMethod]
        public void Trade_node_util__ping()
        {
            var result = _node.Ping();
            result.Dump();
        }
    }
}
