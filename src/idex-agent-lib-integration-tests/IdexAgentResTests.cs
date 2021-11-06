//using binance_lib;
//using config_client_lib;
//using dump_lib;
//using idex_agent_lib;
//using idex_data_lib;
//using idex_integration_lib;
//using log_lib;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Moq;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using trade_node_integration;
//using web_util;

//namespace idex_agent_lib_integration_tests
//{
//    [TestClass]
//    public class IdexAgentResTests
//    {
//        private IdexIntegration _idex;
//        private BinanceIntegration _binance;

//        [TestInitialize]
//        public void Setup()
//        {
//            var webUtil = new WebUtil();
//            var configClient = new ConfigClient();
//            var holdingsRepo = new IdexHoldingsRepo(configClient);
//            var orderBookRepo = new IdexOrderBookRepo(configClient);
//            var openOrdersRepo = new IdexOpenOrdersRepo(configClient);
//            var historyRepo = new IdexHistoryRepo(configClient);
//            var log = new Mock<ILogRepo>();

//            var nodeUtil = new TradeNodeUtil(configClient, webUtil, log.Object);

//            _idex = new IdexIntegration(webUtil, configClient, holdingsRepo, orderBookRepo, openOrdersRepo, historyRepo, log.Object);
//            _binance = new BinanceIntegration(webUtil, configClient, nodeUtil, log.Object);
//        }

//        [TestMethod]
//        public void Idex_agent_lib__compare_idex_commodities_to_binance_symbols()
//        {
//            var binanceCommodities = _binance.GetCommodities();
//            var idexCommodities = _idex.GetCommodities();

//            var intersection = idexCommodities.Where(queryIdexCommodity => binanceCommodities.Any(queryBinanceCommodity =>
//                string.Equals(queryIdexCommodity.Symbol, queryBinanceCommodity.Symbol, StringComparison.InvariantCultureIgnoreCase)
//            )).Select(item => item.Symbol).ToList();

//            var symbolsToAvoid = new List<string> {
//                "ICX", // ICX is moving to their own mainnet.
//                "CTR"  // CTR was accused of fraud
//            };

//            var symbolsToAdd = 
//                intersection.Where(queryIntersection =>
//                !IdexAgentRes.BinanceIntersection.Any(binanceSymbol => string.Equals(binanceSymbol, queryIntersection, StringComparison.InvariantCultureIgnoreCase)))
//                .ToList();

//            symbolsToAdd.Dump();
//        }
//    }
//}
