using config_client_lib;
using coss_api_client_lib;
using coss_api_client_lib.Models;
using date_time_lib;
using dump_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Linq;
using trade_constants;

namespace coss_api_client_lib_tests
{
    [TestClass]
    public class CossApiClientTests
    {
        private ConfigClient _configClient;
        private CossApiClient _client;

        [TestInitialize]
        public void Setup()
        {
            _configClient = new ConfigClient();
            _client = new CossApiClient();
        }

        [TestMethod]
        public void Coss_api_client__get_order_book_raw__eth_btc()
        {
            var contents = _client.GetOrderBookRaw("ETH", "BTC");
            var deserialized = JsonConvert.DeserializeObject<CossEngineOrderBook>(contents);
            deserialized.Dump();
        }

        [TestMethod]
        public void Coss_api_client__get_exchange_info_raw()
        {
            var contents = _client.GetExchangeInfoRaw();
            contents.Dump();
        }

        [TestMethod]
        public void Coss_api_client__get_web_coins_raw()
        {
            var contents = _client.GetWebCoinsRaw();
            contents.Dump();
        }

        [TestMethod]
        public void Coss_api_client__get_completed_orders_raw__eth_btc()
        {
            var apiKey = _configClient.GetApiKey(IntegrationNameRes.Coss);
            var contents = _client.GetCompletedOrdersRaw(apiKey, "ETH", "BTC");

            contents.Dump();
        }

        [TestMethod]
        public void Coss_api_client__get_completed_orders__eth_btc__multiple_pages()
        {
            var apiKey = _configClient.GetApiKey(IntegrationNameRes.Coss);

            for (var i = 0; i < 2; i++)
            {
                var orders = _client.GetCompletedOrders(apiKey, "ETH", "BTC", null, i);
                orders.List.Select(item => new { item.OrderId, TimeStampUtc = DateTimeUtil.UnixTimeStamp13DigitToDateTime( item.CreateTime) }).ToList().Dump();
            }
        }

        [TestMethod]
        public void Coss_api_client__get_server_time_raw()
        {
            var contents = _client.GetServerTimeRaw();
            contents.Dump();
        }

        [TestMethod]
        public void Coss_api_client__synchronize_time()
        {
            _client.SynchronizeTime();
        }

        [TestMethod]
        public void Coss_api_client__account_details()
        {
            var apiKey = _configClient.GetApiKey(IntegrationNameRes.Coss);
            var result = _client.GetAccountDetailsRaw(apiKey);
            result.Dump();
        }
    }
}
