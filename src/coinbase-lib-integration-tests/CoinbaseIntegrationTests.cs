using coinbase_lib;
using dump_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using cache_lib.Models;
using trade_model;
using System.Linq;
using config_client_lib;
using trade_node_integration;
using web_util;
using Moq;
using log_lib;
using System;

namespace coinbase_lib_integration_tests
{
    [TestClass]
    public class CoinbaseIntegrationTests
    {
        private CoinbaseIntegration _coinbase;
        private ITradeNodeUtil _nodeUtil;

        [TestInitialize]
        public void Setup()
        {
            var log = new Mock<ILogRepo>();

            var webUtil = new WebUtil();
            var configClient = new ConfigClient();

            _nodeUtil = new TradeNodeUtil(configClient, webUtil, log.Object);            
            _coinbase = new CoinbaseIntegration(configClient, _nodeUtil, log.Object);
        }

        [TestMethod]
        public void Coinbase__json_history()
        {
            var results = _coinbase.GetJsonHistory();
            // results.Dump();

            var btcAccount = results.SingleOrDefault(queryResult =>
                string.Equals(queryResult.AccountId, "5df2bc90-f8ea-5cfa-a0bf-264c84d11c9e", StringComparison.InvariantCultureIgnoreCase));

            var x = btcAccount.Transactions
                .Where(item => item.CreatedAt.HasValue
                    && item.CreatedAt.Value.Year == 2017
                    && item.CreatedAt.Value.Month == 9
                    && item.CreatedAt.Value.Day == 13);
                //.First();// .Single(tran => string.Equals(tran.Id, "	53ed55f8-5fb5-5e8b-b5d4-ad17dcb89ffb", StringComparison.InvariantCultureIgnoreCase));
            x.Dump();
            // btcAccount.Dump();
        }

        [TestMethod]
        public void Coinbase__get_history_from_json()
        {
            var results = _coinbase.GetHistoryFromJson();
            results.Dump();
        }

        [TestMethod]
        public void Coinbase__get_history()
        {
            var results = _coinbase.GetUserTradeHistory(CachePolicy.ForceRefresh);
            // results.Dump();

            var withdrawals = results.Where(item => item.TradeType == TradeTypeEnum.Withdraw)
                .ToList();

            withdrawals.Dump();
        }

        [TestMethod]
        public void Coinbase__parse_line()
        {
            const string Line = "Withdrawal	600.00 USD			0.00 USD		COMPLETED	December 16, 2017";
            var results = _coinbase.ParseLine(Line);
            results.Dump();
        }

        [TestMethod]
        public void Coinbase__get_user_trade_history_v2()
        {
            var results = _coinbase.GetUserTradeHistoryV2(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Coinbase__get_sells()
        {
            var results = _coinbase.GetSells();
            results.Dump();
        }

        [TestMethod]
        public void Coinbase__get_ltc_deposit_address()
        {
            var address = _coinbase.GetDepositAddress("LTC", CachePolicy.OnlyUseCacheUnlessEmpty);
            address.Dump();
        }
    }
}
