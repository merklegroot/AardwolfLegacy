using dump_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using oex_lib.Client;
using System.Collections.Generic;
using web_util;

namespace oex_tests.Client
{
    [TestClass]
    public class OexClientTests
    {
        private OexClient _client;

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            _client = new OexClient(webUtil);
        }

        [TestMethod]
        public void Oexclient__get_order_book__pgt_btc()
        {
            var result = _client.GetOrderBookRaw(146);
            result.Dump();
        }

        [TestMethod]
        public void Oexclient__get_trade_market_source()
        {
            var source = _client.GetTradeMarketSource();
            source.Dump();
        }
    }
}
