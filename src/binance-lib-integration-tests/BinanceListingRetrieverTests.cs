using binance_lib;
using config_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using web_util;

namespace binance_lib_integration_tests
{
    [TestClass]
    public class BinanceListingRetrieverTests
    {
        private BinanceListingRetriever _binanceListingRetriever;

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            var configRepo = new ConfigRepo();
            _binanceListingRetriever = new BinanceListingRetriever();
        }

        [TestMethod]
        public void Binance__get_listings()
        {
            _binanceListingRetriever.Execute();
        }
    }
}
