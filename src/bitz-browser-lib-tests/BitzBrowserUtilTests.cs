using bitz_browser_lib;
using bitz_data_lib;
using config_client_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using tfa_lib;
using wait_for_it_lib;
using web_util;

namespace bitz_browser_lib_tests
{
    [TestClass]
    public class BitzBrowserUtilTests
    {
        private BitzBrowserUtil _browser;

        [TestInitialize]
        public void Setup()
        {
            var configClient = new ConfigClient();
            var tradeHistoryRepo = new BitzTradeHistoryRepo(configClient);
            var fundsRepo = new BitzFundsRepo(configClient);
            
            var webUtil = new WebUtil();
            var tfaUtil = new TfaUtil(webUtil);
            var waitForIt = new WaitForIt();
            var log = new Mock<ILogRepo>();

            _browser = new BitzBrowserUtil(configClient, tradeHistoryRepo, fundsRepo, tfaUtil, waitForIt, log.Object);
        }

        [TestCleanup]
        public void Teardown()
        {
            _browser.Dispose();
        }

        [TestMethod]
        public void Bitz_browser__browser_login()
        {
            var result = _browser.Login();
            result.ShouldBeTrue();
        }

        [TestMethod]
        public void Bitz_browser__update_funds()
        {
            var result = _browser.UpdateFunds();
            result.ShouldBeTrue();
        }
    }
}
