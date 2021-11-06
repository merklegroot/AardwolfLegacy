using binance_lib;
using integration_workflow_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using trade_lib;
using web_util;
using Moq;
using mew_integration_lib;
using etherscan_lib;
using trade_res;
using test_shared;
using trade_node_integration;
using kucoin_lib;
using trade_email_lib;
using wait_for_it_lib;
using qryptos_lib;
using hitbtc_lib;
using rabbit_lib;
using env_config_lib;
using client_lib;
using config_client_lib;
using exchange_client_lib;

namespace integration_workflow_integration_tests
{
    [TestClass]
    public class TransferFundsWorkflowTests
    {
        private TransferFundsWorkflow _workflow;

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            var dbContext = IntegrationTests.DatabaseContext;
            var configClient = new ConfigClient();
            var etherscanHoldingRepo = new EtherscanHoldingRepo(configClient);
            var ethercanHistoryRepo = new EtherscanHistoryRepo(configClient);
            var emailUtil = new TradeEmailUtil(webUtil);
            var waitForIt = new WaitForIt();
            var rabbitConnectionFactory = new RabbitConnectionFactory(new EnvironmentConfigRepo());

            var log = new Mock<ILogRepo>();

            var nodeUtil = new TradeNodeUtil(configClient, webUtil, log.Object);

            var rabbitConnFactory = new RabbitConnectionFactory(new EnvironmentConfigRepo());
            var validator = new DepositAddressValidator();

            var exchangeClient = new ExchangeClient();

            _workflow = new TransferFundsWorkflow(exchangeClient, validator);
        }

        [TestMethod]
        public void Integration_workflow__transfer_ark_from_binance_to_coss()
        {
            _workflow.Transfer(ExchangeNameRes.Binance, ExchangeNameRes.Coss, CommodityRes.Ambrosous, true);
        }

        [TestMethod]
        public void Integration_workflow__transfer_fota_from_kucoin_to_mew()
        {
            _workflow.Transfer(ExchangeNameRes.KuCoin, ExchangeNameRes.Mew, CommodityRes.Fortuna, true);
        }

        [TestMethod]
        public void Integration_workflow__transfer_bitcoin_from_binance_to_coss()
        {
            var commodity = CommodityRes.Bitcoin;
            bool shouldRun = false;

            // this method will actually withdraw the commodity.
            // prevent this from accidentally being run.
            if (!shouldRun)
            {
                Assert.Inconclusive("This test must be run manually since it actually withdraws 1 WAVES.");
                return;
            }

            _workflow.Transfer(ExchangeNameRes.Binance, ExchangeNameRes.Coss, commodity, true);
        }

        [TestMethod]
        public void Integration_workflow__transfer_dash_from_binance_to_coss()
        {
            var commodity = CommodityRes.Dash;
            bool shouldRun = false;

            // this method will actually withdraw the commodity.
            // prevent this from accidentally being run.
            if (!shouldRun)
            {
                Assert.Inconclusive("This test must be run manually since it actually withdraws 1 WAVES.");
                return;
            }

            _workflow.Transfer(ExchangeNameRes.Binance, ExchangeNameRes.Coss, commodity, true);
        }

        [TestMethod]
        public void Integration_workflow__transfer_eth_from_binance_to_mew()
        {
            var commodity = CommodityRes.Eth;
            bool shouldRun = false;

            // this method will actually withdraw the commodity.
            // prevent this from accidentally being run.
            if (!shouldRun)
            {
                Assert.Inconclusive("This test must be run manually since it actually withdraws funds.");
                return;
            }

            _workflow.Transfer(ExchangeNameRes.Binance, ExchangeNameRes.Mew, commodity, true);
        }

        [TestMethod]
        public void Integration_workflow__transfer_can_from_kucoin_to_qryptos()
        {
            var commodity = CommodityRes.CanYaCoin;
            bool shouldRun = false;

            // this method will actually withdraw the commodity.
            // prevent this from accidentally being run.
            if (!shouldRun)
            {
                Assert.Inconclusive("This test must be run manually since it actually withdraws funds.");
                return;
            }

            _workflow.Transfer(ExchangeNameRes.KuCoin, ExchangeNameRes.Qryptos, commodity, true);
        }

        [TestMethod]
        public void Integration_workflow__transfer_can_from_kucoin_to_coss()
        {
            var commodity = CommodityRes.CanYaCoin;
            bool shouldRun = false;

            // this method will actually withdraw the commodity.
            // prevent this from accidentally being run.
            if (!shouldRun)
            {
                Assert.Inconclusive("This test must be run manually since it actually withdraws funds.");
                return;
            }

            _workflow.Transfer(ExchangeNameRes.KuCoin, ExchangeNameRes.Coss, commodity, true);
        }

        [TestMethod]
        public void Integration_workflow__transfer_lala_from_kucoin_to_coss()
        {
            var commodity = CommodityRes.LaLaWorld;
            bool shouldRun = false;

            // this method will actually withdraw the commodity.
            // prevent this from accidentally being run.
            if (!shouldRun)
            {
                Assert.Inconclusive("This test must be run manually since it actually withdraws funds.");
                return;
            }

            _workflow.Transfer(ExchangeNameRes.KuCoin, ExchangeNameRes.Coss, commodity, true);
        }

        [TestMethod]
        public void Integration_workflow__transfer_ixt_from_hitbtc_to_qryptos()
        {
            var commodity = CommodityRes.IxLedger;
            bool shouldRun = false;

            // this method will actually withdraw the commodity.
            // prevent this from accidentally being run.
            if (!shouldRun)
            {
                Assert.Inconclusive("This test must be run manually since it actually withdraws funds.");
                return;
            }

            _workflow.Transfer(ExchangeNameRes.HitBtc, ExchangeNameRes.Qryptos, commodity, true);
        }
    }
}
