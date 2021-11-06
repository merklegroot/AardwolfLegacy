using binance_lib;
using config_client_lib;
using config_lib;
using dump_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace binance_lib_integration_tests
{
    [TestClass]
    public class BinanceTwitterReaderTests
    {
        private BinanceTwitterReader _twitterReader;

        [TestInitialize]
        public void Setup()
        {
            var configClient = new ConfigClient();
            _twitterReader = new BinanceTwitterReader(configClient);
        }

        [TestMethod]
        public void Binance_twitter_reader__get_binance_listing_tweets()
        {
            var results = _twitterReader.GetBinanceListingTweets();
            results.Dump();
        }
    }
}
