using config_client_lib;
using dump_lib;
using hitbtc_lib.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using web_util;

namespace hitbtc_lib_tests.Client
{
    [TestClass]
    public class HitBtcClientTests
    {
        private HitBtcClient _hitBtcClient;
        private ConfigClient _configClient;

        [TestInitialize]
        public void Setup()
        {
            _configClient = new ConfigClient();

            var webUtil = new WebUtil();
            _hitBtcClient = new HitBtcClient(webUtil);
        }

        [TestMethod]
        public void Hitbtc_client__get_currencies()
        {
            var currencies = _hitBtcClient.GetCurrenciesRaw();
            currencies.Dump();
        }

        [TestMethod]
        public void Hitbtc_client__get_open_orders_raw()
        {
            var apiKey = _configClient.GetHitBtcApiKey();
            var openOrders = _hitBtcClient.GetOpenOrdersRaw(apiKey);

            openOrders.Dump();
        }

        [TestMethod]
        public void Hitbtc_client__get_history_raw()
        {
            var apiKey = _configClient.GetHitBtcApiKey();
            var results = _hitBtcClient.GetTradeHistoryRaw(apiKey);
            results.Dump();
        }

        [TestMethod]
        public void Hitbtc_client__get_history()
        {
            var apiKey = _configClient.GetHitBtcApiKey();
            var results = _hitBtcClient.GetTradeHistory(apiKey);
            results.Dump();
        }

        [TestMethod]
        public void Hitbtc_client__get_transactions_history_raw()
        {
            var apiKey = _configClient.GetHitBtcApiKey();
            var results = _hitBtcClient.GetTransactionsHistoryRaw(apiKey);
            results.Dump();
        }

        [TestMethod]
        public void Hitbtc_client__get_transactions_history()
        {
            var apiKey = _configClient.GetHitBtcApiKey();
            var results = _hitBtcClient.GetTransactionsHistory(apiKey);

            results.Dump();
        }
    }
}
