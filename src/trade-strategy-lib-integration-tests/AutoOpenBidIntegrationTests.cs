//using cryptocompare_lib;
//using dump_lib;
//using env_config_lib;
//using hitbtc_lib;
//using idex_data_lib;
//using idex_integration_lib;
//using kucoin_lib;
//using log_lib;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Moq;
//using rabbit_lib;
//using Shouldly;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using trade_email_lib;
//using trade_model;
//using trade_node_integration;
//using trade_strategy_lib;
//using wait_for_it_lib;
//using web_util;
//using cache_lib.Models;
//using config_client_lib;
//using browser_automation_client_lib;
//using kucoin_lib.Client;
//using hitbtc_lib.Client;

//namespace trade_strategy_lib_integration_tests
//{
//    [TestClass]
//    public class AutoOpenBidIntergrationTests
//    {
//        private IdexIntegration _idexIntegration;
//        private IKucoinIntegration _kucoinIntegration;
//        private IHitBtcIntegration _hitBtcIntegration;
//        private ICryptoCompareIntegration _cryptoCompareIntegration;

//        private AutoOpenBid _autoOpenBid;

//        [TestInitialize]
//        public void Setup()
//        {
//            var webUtil = new WebUtil();
//            var configClient = new ConfigClient();
//            var waitForIt = new WaitForIt();
//            var log = new Mock<ILogRepo>();
//            var emailUtil = new TradeEmailUtil(webUtil);
//            var nodeUtil = new TradeNodeUtil(configClient, webUtil, log.Object);

//            var idexHoldingsRepo = new IdexHoldingsRepo(configClient);
//            var idexOrderBookRepo = new IdexOrderBookRepo(configClient);
//            var idexOpenOrdersRepo = new IdexOpenOrdersRepo(configClient);
//            var idexHistoryRepo = new IdexHistoryRepo(configClient);
//            var rabbitConnectionFactory = new RabbitConnectionFactory(new EnvironmentConfigRepo());
//            var browserAutomationClient = new BrowserAutomationClient();
            
//            _idexIntegration = new IdexIntegration(webUtil, configClient, idexHoldingsRepo, idexOrderBookRepo, idexOpenOrdersRepo, idexHistoryRepo, log.Object);
//            var kucoinClient = new KucoinClient(webUtil);
//            _kucoinIntegration = new KucoinIntegration(kucoinClient, nodeUtil, emailUtil, webUtil, configClient, waitForIt, rabbitConnectionFactory, log.Object);

//            var hitBtcClient = new HitBtcClient(webUtil);
//            _hitBtcIntegration = new HitBtcIntegration(
//                webUtil,
//                configClient,
//                hitBtcClient,
//                nodeUtil, browserAutomationClient, log.Object);
//            _cryptoCompareIntegration = new CryptoCompareIntegration(webUtil, configClient);

//            _autoOpenBid = new AutoOpenBid();
//        }

//        [TestMethod]
//        public void Auto_open_bid__idex_vs_kucoin_and_hitbtc()
//        {
//            const string Symbol = "PAY";
//            var tradingPair = new TradingPair(Symbol, "ETH");

//            var idexOrderBookTask = Task.Run(() => _idexIntegration.GetOrderBookFromApi(tradingPair, CachePolicy.ForceRefresh));
//            var kucoinOrderBookTask = Task.Run(() => _kucoinIntegration.GetOrderBook(tradingPair, CachePolicy.ForceRefresh));
//            var hitBtcOrderBookTask = Task.Run(() => _hitBtcIntegration.GetOrderBook(tradingPair, CachePolicy.ForceRefresh));
//            var cryptoCompareTask = Task.Run(() => _cryptoCompareIntegration.GetPrice(Symbol, "ETH", CachePolicy.ForceRefresh));

//            var comps = new List<OrderBook> { kucoinOrderBookTask.Result, hitBtcOrderBookTask.Result };

//            var cryptoComparePrice = cryptoCompareTask.Result;
//            cryptoComparePrice.ShouldNotBeNull();

//            var idexOrderBook = idexOrderBookTask.Result;

//            var result = _autoOpenBid.ExecuteAgainstRegularExchanges(idexOrderBook, comps, cryptoComparePrice.Value);
//            result.Dump();
//        }
//    }
//}
