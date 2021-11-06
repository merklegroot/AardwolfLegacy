using dump_lib;
using gemini_lib.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using web_util;

namespace gemini_lib_tests.Client
{
    [TestClass]
    public class GeminiClientTests
    {
        private GeminiClient _client;

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            _client = new GeminiClient(webUtil);
        }

        [TestMethod]
        public void Gemini_client__get_symbols()
        {
            var results = _client.GetSymbols();
            results.Dump();
        }

        [TestMethod]
        public void Gemini_client__get_order_book__eth_usd()
        {
            var results = _client.GetOrderBook("ETH", "USD");
            results.Dump();
        }
    }
}
