using config_client_lib;
using dump_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using qryptos_lib.Client;

namespace qryptos_integration_tests.Client
{
    [TestClass]
    public class QryptosClientTests
    {
        private ConfigClient _configClient;
        private QryptosClient _client;

        [TestInitialize]
        public void Setup()
        {
            _configClient = new ConfigClient();
            _client = new QryptosClient();
        }

        [TestMethod]
        public void Qryptos_client__get_account__qash()
        {
            const string Symbol = "QASH";

            var apiKey = _configClient.GetQryptosApiKey();
            var result = _client.GetAccount(apiKey, Symbol);

            result.Dump();
        }

        [TestMethod]
        public void Qryptos_client__get_accounts()
        {
            var apiKey = _configClient.GetQryptosApiKey();
            var result = _client.PerformAuthRequest(apiKey, "/accounts");

            result.Dump();
        }
    }
}
