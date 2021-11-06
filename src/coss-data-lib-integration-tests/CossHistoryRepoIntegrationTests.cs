using coss_data_lib;
using dump_lib;
using config_client_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace coss_data_lib_integration_tests
{
    [TestClass]
    public class CossHistoryRepoIntegrationTests
    {
        private CossHistoryRepo _repo;

        [TestInitialize]
        public void Setup()
        {
            var configClient = new ConfigClient();
            _repo = new CossHistoryRepo(configClient);
        }

        [TestMethod]
        public void Coss_history_repo__get()
        {
            var results = _repo.Get();
            results.Dump();
        }
    }
}
