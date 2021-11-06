using binance_lib;
using config_lib;
using coss_data_lib;
using coss_lib;
using cryptocompare_lib;
using dump_lib;
using idex_data_lib;
using idex_integration_lib;
using integration_workflow_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using res_util_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using trade_lib;
using trade_model;
using trade_node_integration;
using web_util;

namespace integration_workflow_integration_tests
{
    [TestClass]
    public class ArbitrageWorkflowTests
    {
        private ArbitrageWorkflow _workflow;
        private ITradeIntegration _binance;
        private ITradeIntegration _coss;
        private ITradeIntegration _idex;
        private CryptoCompareIntegration _cryptoCompare;

        private decimal _btcToUsd;

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            var configRepo = new ConfigRepo();
            var cossHistoryRepo = new CossHistoryRepo(configRepo);
            var cossOpenOrderRepo = new CossOpenOrderRepo(configRepo);

            var log = new Mock<ILogRepo>();
            var nodeUtil = new TradeNodeUtil(configRepo, webUtil, log.Object);

            _cryptoCompare = new CryptoCompareIntegration(webUtil, configRepo);
            _binance = new BinanceIntegration(webUtil, configRepo, configRepo, nodeUtil, log.Object);            
            _coss = new CossIntegration(webUtil, cossHistoryRepo, cossOpenOrderRepo, configRepo, log.Object);
            var idexHoldingsRepo = new IdexHoldingsRepo(configRepo);
            var idexOrderBookRepo = new IdexOrderBookRepo(configRepo);
            var idexOpenOrdersRepo = new IdexOpenOrdersRepo(configRepo);
            var idexHistoryRepo = new IdexHistoryRepo(configRepo);
            _idex = new IdexIntegration(webUtil, configRepo, configRepo, idexHoldingsRepo, idexOrderBookRepo, idexOpenOrdersRepo, idexHistoryRepo, log.Object);

            _btcToUsd = _cryptoCompare.GetUsdValue("BTC", CachePolicy.OnlyUseCacheUnlessEmpty) ?? 0;

            _workflow = new ArbitrageWorkflow(_cryptoCompare);
        }

        [TestMethod]
        public void Arbitrage_workflow__basic_scenario()
        {
            var data = new ArbitrageData
            {
                EthToBtcRatio = 0.1m,
                BtcToUsdRatio = 8000.0m,
                SourceWithdrawalFee = 1,

                SourceBtcOrderBook = new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Quantity = 10, Price = 0.1m }
                    }
                },
                SourceEthOrderBook = new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Quantity = 20, Price = 1.1m }
                    }
                },
                
                DestBtcOrderBook = new OrderBook
                {
                    Bids = new List<Order>
                    {
                        new Order { Quantity = 5m, Price = 0.11m }
                    }
                },
                DestEthOrderBook = new OrderBook
                {
                    Bids = new List<Order>
                    {
                        new Order { Quantity = 15m, Price = 1.2m }
                    }
                }
            };

            var result = _workflow.Execute(data);

            result.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__from_json_data()
        {
            var data = ResUtil.Get<ArbitrageData>("arbitrage-data.json", GetType().Assembly);
            var results = _workflow.Execute(data);

            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__bitcoin_cash()
        {
            var results = _workflow.Execute(_binance, _coss, "BCH", CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__link()
        {
            var results = _workflow.Execute(_binance, _coss, "LINK", CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_binance_to_coss()
        {
            Arbitrage_workflow(_binance, _coss);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_coss_to_binance()
        {
            Arbitrage_workflow(_coss, _binance);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_idex_to_binane()
        {
            var cachePolicy = CachePolicy.ForceRefresh;

            var source = _idex;
            var dest = _binance;

            var sourceTradingPairsTask = Task.Run(() => source.GetTradingPairs(cachePolicy));
            var destTradingPairsTask = Task.Run(() => dest.GetTradingPairs(cachePolicy));
            var destTradingPairs = destTradingPairsTask.Result;

            var sourceTradingPairs = sourceTradingPairsTask.Result;

            var intersections = sourceTradingPairs.Where(sourcePair => destTradingPairs.Any(destPair => string.Equals(sourcePair.Symbol, destPair.Symbol, StringComparison.InvariantCultureIgnoreCase))).ToList();

            intersections.Select(item => item.Symbol).ToList().Dump();

            for (var i = 0; i < intersections.Count; i++)
            {
                var tradingPair = intersections[i];
                var binanceOrderBook = _binance.GetOrderBook(tradingPair, CachePolicy.AllowCache);
            }
        }

        public void Arbitrage_workflow(ITradeIntegration source, ITradeIntegration dest, CachePolicy cachePolicy = CachePolicy.ForceRefresh)
        {
            var sourceTradingPairs = source.GetTradingPairs(cachePolicy);
            var destTradingPairs = dest.GetTradingPairs(cachePolicy);

            var intersections = sourceTradingPairs.Intersect(destTradingPairs).ToList()
                .Where(item => item.Symbol != "ETH").ToList();

            var symbolsWithBoth = intersections.Where(item =>
                item.BaseSymbol == "BTC"
                && intersections.Any(queryTradingPair => queryTradingPair.Equals(new TradingPair(item.Symbol, "ETH"))))
                .Select(item => item.Symbol)
                .OrderBy(item => item)
                .ToList();

            bool wereAnyProfitsFound = false;
            foreach (var symbol in symbolsWithBoth)
            {
                var result = _workflow.Execute(source, dest, symbol, cachePolicy);
                if (result.TotalQuantity > 0)
                {
                    wereAnyProfitsFound = true;
                    Console.WriteLine($"Profit of ${result.ExpectedUsdProfit} found for [{symbol}].");
                    result.Dump();
                }
                else
                {
                    Console.WriteLine($"No profit found for [{symbol}].");
                }
            }

            if (!wereAnyProfitsFound) { Console.WriteLine("No profits found."); }
        }
    }
}
