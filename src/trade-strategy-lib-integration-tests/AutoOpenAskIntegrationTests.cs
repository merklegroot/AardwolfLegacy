using binance_lib;
using cache_lib.Models;
using cryptocompare_lib;
using dump_lib;
using env_config_lib;
using hitbtc_lib;
using idex_data_lib;
using idex_integration_lib;
using client_lib;
using kucoin_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using rabbit_lib;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using trade_email_lib;
using trade_model;
using trade_node_integration;
using trade_strategy_lib;
using wait_for_it_lib;
using web_util;
using config_client_lib;
using browser_automation_client_lib;
using exchange_client_lib;
using trade_res;

namespace trade_strategy_lib_integration_tests
{
    [TestClass]
    public class AutoOpenAskIntegrationTests
    {
        // private IdexIntegration _idexIntegration;
        // private BinanceIntegration _binanceIntegration;
        // private IKucoinIntegration _kucoinIntegration;
        // private IHitBtcIntegration _hitBtcIntegration;

        private IExchangeClient _exchangeClient;
        private ICryptoCompareIntegration _cryptoCompareIntegration;

        private AutoOpenAsk _autoOpenAsk;

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            var configClient = new ConfigClient();
            var waitForIt = new WaitForIt();
            var log = new Mock<ILogRepo>();
            var emailUtil = new TradeEmailUtil(webUtil);
            var nodeUtil = new TradeNodeUtil(configClient, webUtil, log.Object);

            var idexHoldingsRepo = new IdexHoldingsRepo(configClient);
            var idexOrderBookRepo = new IdexOrderBookRepo(configClient);
            var idexOpenOrdersRepo = new IdexOpenOrdersRepo(configClient);
            var idexHistoryRepo = new IdexHistoryRepo(configClient);
            var rabbitConnectionFactory = new RabbitConnectionFactory(new EnvironmentConfigRepo());
            var browserAutomationClient = new BrowserAutomationClient();

            // _idexIntegration = new IdexIntegration(webUtil, configClient, idexHoldingsRepo, idexOrderBookRepo, idexOpenOrdersRepo, idexHistoryRepo, log.Object);
            // _kucoinIntegration = new KucoinIntegration(nodeUtil, emailUtil, webUtil, configClient, waitForIt, rabbitConnectionFactory, log.Object);
            // _hitBtcIntegration = new HitBtcIntegration(webUtil, configClient, nodeUtil, browserAutomationClient, log.Object);
            // _binanceIntegration = new BinanceIntegration(webUtil, configClient, nodeUtil, log.Object);

            _exchangeClient = new ExchangeClient();
            _cryptoCompareIntegration = new CryptoCompareIntegration(webUtil, configClient);

            _autoOpenAsk = new AutoOpenAsk();
        }

        [TestMethod]
        public void Auto_open_ask__idex_vs_binance()
        {
            const string Symbol = "KNC";
            const string BaseSymbol = "ETH";

            var idexOrderBook = _exchangeClient.GetOrderBook(ExchangeNameRes.Idex, Symbol, BaseSymbol, CachePolicy.ForceRefresh);

            var binanceOrderBook = _exchangeClient.GetOrderBook(ExchangeNameRes.Binance, Symbol, BaseSymbol, CachePolicy.ForceRefresh);

            var result = _autoOpenAsk.ExecuteAgainstHighVolumeExchange(
                idexOrderBook,
                binanceOrderBook);

            result.Dump();
        }

        [TestMethod]
        public void Auto_open_ask__idex_vs_kucoin_and_hitbtc()
        {
            const string Symbol = "PAY";
            const string BaseSymbol = "ETH";

            var idexOrderBookTask = Task.Run(() => _exchangeClient.GetOrderBook(ExchangeNameRes.Idex, Symbol, BaseSymbol, CachePolicy.ForceRefresh));
            var kucoinOrderBookTask = Task.Run(() => _exchangeClient.GetOrderBook(ExchangeNameRes.KuCoin, Symbol, BaseSymbol, CachePolicy.ForceRefresh));
            var hitBtcOrderBookTask = Task.Run(() => _exchangeClient.GetOrderBook(ExchangeNameRes.HitBtc, Symbol, BaseSymbol, CachePolicy.ForceRefresh));

            var cryptoCompareTask = Task.Run(() => _cryptoCompareIntegration.GetPrice(Symbol, "ETH", CachePolicy.ForceRefresh));

            var comps = new List<OrderBook> { kucoinOrderBookTask.Result, hitBtcOrderBookTask.Result };

            var cryptoComparePrice = cryptoCompareTask.Result;
            cryptoComparePrice.ShouldNotBeNull();

            var idexOrderBook = idexOrderBookTask.Result;

            var result = _autoOpenAsk.ExecuteAgainstRegularExchanges(idexOrderBook, comps, cryptoComparePrice.Value);
            result.Dump();
        }
    }
}
