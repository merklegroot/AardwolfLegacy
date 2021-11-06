using config_client_lib;
using etherscan_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using web_util;

namespace etherscan_integration_tests
{
    [TestClass]
    public class EtherscanIntegrationTests
    {
        private EtherscanIntegration _integration;

        [TestInitialize]
        public void Setup()
        {
            var configClient = new ConfigClient();
            var webUtil = new WebUtil();
            _integration = new EtherscanIntegration(configClient, webUtil);
        }
    }
}
