using dump_lib;
using etherscan_lib;
using config_client_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace etherscan_integration_tests
{
    [TestClass]
    public class EtherscanRepoTests
    {
        private EtherscanHoldingRepo _repo;

        [TestInitialize]
        public void Setup()
        {
            var configClient = new ConfigClient();
            _repo = new EtherscanHoldingRepo(configClient);
        }

        [TestMethod]
        public void Etherscan_repo__get_most_recent()
        {
            var results = _repo.Get();
            results.Dump();
        }
    }
}
