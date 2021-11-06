using config_client_lib;
using dump_lib;
using kucoin_lib.Client;
using KucoinClientModelLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using trade_model;
using web_util;

namespace kucoin_integration_tests.Client
{
    [TestClass]
    public class KucoinClientTests
    {
        private ApiKey _apiKey;
        private KucoinApiKey _kucoinApiKey;
        private KucoinClient _kucoinClient;

        [TestInitialize]
        public void Setup()
        {
            var configClient = new ConfigClient();
            _apiKey = configClient.GetKucoinApiKey();
            var apiPassphrase = configClient.GetKucoinApiPassphrase();
            _kucoinApiKey = new KucoinApiKey
            {
                PublicKey = _apiKey.Key,
                PrivateKey = _apiKey.Secret,
                Passphrase = apiPassphrase
            };

            var webUtil = new WebUtil();
            _kucoinClient = new KucoinClient(webUtil);
        }

        [TestMethod]
        public void Kucoin_client__limit_buy__can_eth()
        {
            var results = _kucoinClient.CreateOrderRaw(_apiKey, "CAN", "ETH", 0.0002424m, 5.0m, true);
            results.Dump();
        }

        [TestMethod]
        public void Kucoin_client__get_accounts()
        {
            var results = _kucoinClient.GetAccounts(_kucoinApiKey);
            results.Dump();
        }

        [TestMethod]
        public void Kucoin_client__get_server_time_raw()
        {
            var results = _kucoinClient.GetServerTimeRaw();
            results.Dump();
        }

        [TestMethod]
        public void Kucoin_client__get_eth_ledger()
        {
            var accountsResponse = _kucoinClient.GetAccounts(_kucoinApiKey);
            var matchingAccount = accountsResponse.Data.Single(queryAccount => string.Equals(queryAccount.Currency, "ETH"));

            var accountId = matchingAccount.Id;

            var results = _kucoinClient.GetAccountLedgersRaw(_kucoinApiKey, accountId);
            results.Dump();
        }

        [TestMethod]
        public void Kucoin_client__get_fills_raw()
        {
            var results = _kucoinClient.GetFillsRaw(_kucoinApiKey);
            results.Dump();
        }

        [TestMethod]
        public void Kucoin_client__get_historical_orders_raw()
        {
            var results = _kucoinClient.GetHistoricalOrdersRaw(_kucoinApiKey);
            results.Dump();
        }

        [TestMethod]
        public void Kucoin_client__get_currencies_raw()
        {
            _kucoinClient.GetCurrenciesRaw().Dump();
        }

        [TestMethod]
        public void Kucoin_client__get_historical_orders()
        {
            const int MaxPages = 100;
            for (var currentPage = 1; currentPage <= MaxPages; currentPage++)
            {
                if (currentPage != 1) { Thread.Sleep(TimeSpan.FromSeconds(5)); }

                var raw = _kucoinClient.GetHistoricalOrdersRaw(_kucoinApiKey, currentPage);
                var response = JsonConvert.DeserializeObject<KucoinClientGetTradeHistoryResponse>(raw);

                if (currentPage >= response.Data.TotalPage)
                {
                    break;
                }
            }
        }

        [TestMethod]
        public void Kucoin_client__get_v1_historical_withdrawals_raw()
        {
            const int MaxPages = 100;
            for (var currentPage = 1; currentPage <= MaxPages; currentPage++)
            {
                if (currentPage != 1) { Thread.Sleep(TimeSpan.FromSeconds(5)); }

                var raw = _kucoinClient.GetV1HistoricalWithdrawalsRaw(_kucoinApiKey, currentPage);
                var response = JsonConvert.DeserializeObject<KucoinClientGetWithdrawalHistoryResponse>(raw);

                if (currentPage >= response.Data.TotalPage)
                {
                    break;
                }
            }
        }

        [TestMethod]
        public void Kucoin_client__get_historical_withdrawals()
        {
            var raw = _kucoinClient.GetCurrentWithdrawalsRaw(_kucoinApiKey);
            raw.Dump();
        }
    }
}
