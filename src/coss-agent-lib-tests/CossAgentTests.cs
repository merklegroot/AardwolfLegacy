using binance_lib;
using cache_lib.Models;
using coss_agent_lib;
using coss_agent_lib.Strategy;
using coss_lib;
using exchange_client_lib;
using integration_workflow_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OpenQA.Selenium;
using rabbit_lib;
using sel_lib;
using trade_browser_lib;
using trade_email_lib;
using trade_model;
using trade_res;
using wait_for_it_lib;

namespace coss_agent_lib_tests
{
    [TestClass]
    public class CossAgentTests
    {
        private CossAgent _cossAgent;

        [TestInitialize]
        public void Setup()
        {
            var waitForIt = new Mock<IWaitForIt>();
            var cossIntegration = new Mock<ICossIntegration>();

            var binanceIntegration = new Mock<IBinanceIntegration>();
            var arbitrageWorkflow = new Mock<IArbitrageWorkflow>();
            arbitrageWorkflow.Setup(mock => mock.Execute(
                ExchangeNameRes.Coss,
                ExchangeNameRes.Binance,
                "ZEN",
                It.IsAny<CachePolicy>()))
                .Returns(() => new ArbitrageResult
                {
                      EthQuantity = 2.00000015m,
                      EthPrice = 0.03961191m,
                      BtcQuantity = 1.95978571m,
                      BtcPrice =  0.00280000m,
                      ExpectedUsdProfit = 0.53192747458520063557632m
                });

            var nav = new Mock<INavigation>();

            var webDriver = new Mock<IRemoteWebDriver>();
            webDriver.Setup(mock => mock.Navigate()).Returns(nav.Object);

            var cossWebDriverFactory = new Mock<ICossWebDriverFactory>();
            cossWebDriverFactory.Setup(mock => mock.Create())
                .Returns(() => webDriver.Object);

            var cossDriver = new Mock<ICossDriver>();
            cossDriver.Setup(mock => mock.CheckWallet()).Returns(true);

            var cossAutoBuy = new Mock<ICossAutoBuy>();
            var cossAutoOpenBid = new Mock<ICossAutoOpenBid>();
            var exchangeClient = new Mock<IExchangeClient>();

            var emailUtil = new Mock<ITradeEmailUtil>();

            var rabbitConnection = new Mock<IRabbitConnection>();
            var rabbitConnectionFactory = new Mock<IRabbitConnectionFactory>();
            rabbitConnectionFactory.Setup(mock => mock.Connect())
                .Returns(rabbitConnection.Object);

            var log = new Mock<ILogRepo>();

            _cossAgent = new CossAgent(
                waitForIt.Object,
                cossIntegration.Object,
                null, null, null,
                null, null, null,
                null, null,
                cossWebDriverFactory.Object,
                rabbitConnectionFactory.Object,
                cossDriver.Object,
                arbitrageWorkflow.Object,
                emailUtil.Object,
                exchangeClient.Object,
                cossAutoBuy.Object,
                cossAutoOpenBid.Object,
                log.Object);
        }

        [TestMethod]
        public void Coss_agent__start()
        {
            _cossAgent.PerformArbitrage();
        }
    }
}
