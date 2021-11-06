using coss_agent_lib;
using coss_agent_lib.Strategy;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using config_client_lib;
using exchange_client_lib;

namespace coss_agent_integration_tests
{
    [TestClass]
    public class CossAutoBuyTests
    {
        private Mock<ICossDriver> _driver;
        private CossAutoBuy _cossAutoBuy;

        [TestInitialize]
        public void Setup()
        {
            _driver = new Mock<ICossDriver>();
            var configClient = new ConfigClient();
            var exchangeClient = new ExchangeClient();
            var log = new Mock<ILogRepo>();

            _cossAutoBuy = new CossAutoBuy(
                _driver.Object,
                configClient,
                exchangeClient,
                log.Object);
        }

        [TestMethod]
        public void Coss_auto_buy__execute()
        {
            _cossAutoBuy.Execute();
        }
    }
}
