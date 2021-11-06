using dump_lib;
using livecoin_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace livecoin_integration_tests
{
    [TestClass]
    public class LivecoinImportRepoTests
    {
        private LivecoinImportRepo _repo;

        [TestInitialize]
        public void Initialize()
        {
            _repo = new LivecoinImportRepo();
        }

        [TestMethod]
        public void Get_livecoin_import_repo_items()
        {
            _repo.Get().Dump();
        }
    }
}
