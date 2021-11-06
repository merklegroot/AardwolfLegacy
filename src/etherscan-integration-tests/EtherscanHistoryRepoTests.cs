using config_client_lib;
using dump_lib;
using etherscan_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace etherscan_integration_tests
{
    [TestClass]
    public class EtherscanHistoryRepoTests
    {
        private EtherscanHistoryRepo _repo;

        [TestInitialize]
        public void Setup()
        {
            var configClient = new ConfigClient();
            _repo = new EtherscanHistoryRepo(configClient);
        }

        [TestMethod]
        public void Etherscan__get_history()
        {
            var result = _repo.Get();
            result.Dump();
        }
    }
}
