using cache_lib.Models;
using dump_lib;
using env_config_lib;
using etherscan_lib;
using client_lib;
using mew_integration_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using rabbit_lib;
using trade_model;
using config_client_lib;

namespace mew_integration_tests
{
    [TestClass]
    public class MewIntegrationTests
    {
        private MewIntegration _integration;

        [TestInitialize]
        public void Setup()
        {
            var configClient = new ConfigClient();
            var etherscanHoldingRepo = new EtherscanHoldingRepo(configClient);
            var etherscanHistoryRepo = new EtherscanHistoryRepo(configClient);
            var envConfig = new EnvironmentConfigRepo();
            var rabbitConnectionFactory = new RabbitConnectionFactory(envConfig);
            _integration = new MewIntegration(configClient, etherscanHoldingRepo, etherscanHistoryRepo, rabbitConnectionFactory);
        }

        [TestMethod]
        public void Mew__get_eth_deposit_address()
        {
            var result = _integration.GetDepositAddress("ETH", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Mew__get_holdings()
        {
            var results = _integration.GetHoldings(CachePolicy.AllowCache);
            results.Dump();
        }
    }
}
