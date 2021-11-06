using config_client_lib;
using dump_lib;
using livecoin_lib.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using web_util;

namespace livecoin_integration_tests.Client
{
    [TestClass]
    public class LivecoinClientTests
    {
        private IConfigClient _configClient;
        private LivecoinClient _client;

        [TestInitialize]
        public void Setup()
        {
            _configClient = new ConfigClient();
            var webUtil = new WebUtil();
            _client = new LivecoinClient(webUtil);
        }

        [TestMethod]
        public void Livecoin_client__get_order_book__eth_btc()
        {
            var results = _client.GetOrderBook("ETH", "BTC");
            results.Dump();
        }

        [TestMethod]
        public void Livecoin_client__get_commission()
        {
            var apiKey = _configClient.GetLivecoinApiKey();
            var results = _client.GetCommission(apiKey);

            results.Dump();
        }

        [TestMethod]
        public void Livecoin_client__get_balance()
        {
            var apiKey = _configClient.GetLivecoinApiKey();
            var results = _client.GetBalanceRaw(apiKey);

            results.Dump();
        }

          //{
          //  "Symbol": "REP",
          //  "BaseSymbol": "ETH",
          //  "OrderId": "28856418801",
          //  "Price": 0.06236743,
          //  "Quantity": 8.01700502,
          //  "OrderType": 1,
          //  "OrderTypeText": "Bid"
          //},
          //{
          //  "Symbol": "REP",
          //  "BaseSymbol": "ETH",
          //  "OrderId": "28768684301",
          //  "Price": 0.052,
          //  "Quantity": 0.1,
          //  "OrderType": 1,
          //  "OrderTypeText": "Bid"
          //}

        [TestMethod]
        public void Livecoin_client__cancel_order()
        {
            const string OrderId = "28856418801";
            const string NativeSymbol = "REP";
            const string NativeBaseSymbol = "ETH";

            var apiKey = _configClient.GetLivecoinApiKey();
            var contents = _client.CancelOrderRaw(apiKey, NativeSymbol, NativeBaseSymbol, OrderId);

            contents.Dump();
        }

        [TestMethod]
        public void Livecoin_client__get_history()
        {
            var apiKey = _configClient.GetLivecoinApiKey();
            var contents = _client.GetHistoryRaw(apiKey);
            contents.Dump();
        }

        [TestMethod]
        public void Livecoin_client__get_open_orders()
        {
            var apiKey = _configClient.GetLivecoinApiKey();
            var contents = _client.GetOpenOrdersRaw(apiKey);
            contents.Dump();
        }
    }
}
