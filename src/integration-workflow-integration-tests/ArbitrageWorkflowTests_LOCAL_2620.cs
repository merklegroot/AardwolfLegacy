using binance_lib;
using config_lib;
using coss_data_lib;
using coss_lib;
using cryptocompare_lib;
using dump_lib;
using idex_data_lib;
using idex_integration_lib;
using integration_workflow_lib;
using kucoin_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using res_util_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using trade_email_lib;
using trade_lib;
using trade_model;
using trade_node_integration;
using trade_res;
using wait_for_it_lib;
using web_util;

namespace integration_workflow_integration_tests
{
    [TestClass]
    public class ArbitrageWorkflowTests
    {
        private ArbitrageWorkflow _workflow;
        private IBinanceIntegration _binance;
        private ICossIntegration _coss;
        private IKucoinIntegration _kucoin;
        private IIdexIntegration _idex;
        private CryptoCompareIntegration _cryptoCompare;

        private decimal _btcToUsd;

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            var configRepo = new ConfigRepo();
            var cossHistoryRepo = new CossHistoryRepo(configRepo);
            var cossOpenOrderRepo = new CossOpenOrderRepo(configRepo);
            var emailUtil = new TradeEmailUtil(webUtil);
            var waitForIt = new WaitForIt();

            var log = new Mock<ILogRepo>();
            var nodeUtil = new TradeNodeUtil(configRepo, webUtil, log.Object);

            _cryptoCompare = new CryptoCompareIntegration(webUtil, configRepo);
            _binance = new BinanceIntegration(webUtil, configRepo, configRepo, nodeUtil, log.Object);            
            _coss = new CossIntegration(webUtil, cossHistoryRepo, cossOpenOrderRepo, configRepo, log.Object);
            _kucoin = new KucoinIntegration(nodeUtil, emailUtil, webUtil, configRepo, waitForIt, log.Object);

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
        public void Arbitrage_workflow__ark__binance_to_coss()
        {
            var results = _workflow.Execute(_binance, _coss, "ARK", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_binance_to_coss__allow_cache()
        {
            Arbitrage_workflow(_binance, _coss, CachePolicy.AllowCache);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_coss_to_binance__allow_cache()
        {
            Arbitrage_workflow(_coss, _binance, CachePolicy.AllowCache);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_coss_to_binance__force_refresh()
        {
            Arbitrage_workflow(_coss, _binance, CachePolicy.ForceRefresh);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_coss_to_kucoin__allow_cache()
        {
            Arbitrage_workflow(_coss, _kucoin, CachePolicy.AllowCache);
        }

        [TestMethod]
        public void Arbitrage_workflow__check_all_kucoin_to_coss__allow_cache()
        {
            Arbitrage_workflow(_kucoin, _coss, CachePolicy.AllowCache);
        }

        [TestMethod]
        public void Arbitrage_workflow__act_on_arbitrage_result__knc__kucoin_to_coss()
        {
            var commodity = CommodityRes.Knc;

            Act_on_arbitrage_result(_kucoin, _coss, commodity);
        }

        private static bool ShouldCommit = false;

        public void Act_on_arbitrage_result<T>(
            T source,
            ITradeIntegration destination,
            Commodity commodity)

            where T : 
                ITradeIntegration,
                IBuyAndSellIntegration,
                IWithdrawableTradeIntegration
        {
            if (!ShouldCommit)
            {
                Assert.Fail($"This test actually buys things. Turn on \"{nameof(ShouldCommit)}\" to commit.");
                return;
            }

            var results = _workflow.Execute(source, destination, commodity.Symbol, CachePolicy.AllowCache);
            if (results.TotalQuantity <= 0)
            {
                Console.WriteLine("No profits.");
                return;
            }

            results = _workflow.Execute(source, destination, commodity.Symbol, CachePolicy.ForceRefresh);

            if (results.TotalQuantity <= 0)
            {
                Console.WriteLine("No profits.");
                return;
            }
            if (results.EthQuantity > 0)
            {
                var tradingPair = new TradingPair(commodity.Symbol, "ETH");
                var quantityAndPrice = new QuantityAndPrice
                {
                    Quantity = results.EthQuantity,
                    Price = results.EthPrice.Value
                };

                source.BuyLimit(tradingPair, quantityAndPrice);
            }

            if (results.BtcQuantity > 0)
            {
                var tradingPair = new TradingPair(commodity.Symbol, "BTC");
                var quantityAndPrice = new QuantityAndPrice
                {
                    Quantity = results.BtcQuantity,
                    Price = results.BtcPrice.Value
                };

                source.BuyLimit(tradingPair, quantityAndPrice);
            }


            var holding = source.GetHolding(commodity.Symbol, CachePolicy.ForceRefresh);
            var available = holding.Available;

            if (available > 0)
            {
                var withdrawalFee = source.GetWithdrawalFee(commodity.Symbol, CachePolicy.ForceRefresh);

                var depositAddress = destination.GetDepositAddress(commodity.Symbol, CachePolicy.ForceRefresh);
                var quantityToWithdraw = available - withdrawalFee.Value;
                var withdrawalResult = source.Withdraw(commodity, quantityToWithdraw, depositAddress);

                withdrawalResult.Dump();
            }
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
            var sourceTradingPairsTask = Task.Run(() => source.GetTradingPairs(cachePolicy));
            var destTradingPairsTask = Task.Run(() => dest.GetTradingPairs(cachePolicy));

            var sourceTradingPairs = sourceTradingPairsTask.Result;
            var destTradingPairs = destTradingPairsTask.Result;

            var intersections = sourceTradingPairs
                .Where(sourceItem => destTradingPairs.Any(destItem => sourceItem.Equals(destItem)))
                // .Intersect(destTradingPairs).ToList()
                .Where(item => item.Symbol != "ETH").ToList();

            var symbolsWithBoth = intersections.Where(item =>
                item.BaseSymbol == "BTC"
                && intersections.Any(queryTradingPair => queryTradingPair.Equals(new TradingPair(item.Symbol, "ETH"))))
                .Select(item => item.Symbol)
                .OrderBy(item => item)
                .ToList();

            bool wereAnyProfitsFound = false;
            var exceptions = new List<Exception>();
            foreach (var symbol in symbolsWithBoth)
            {
                try
                {
                    var result = _workflow.Execute(source, dest, symbol, cachePolicy);
                    if (result.TotalQuantity > 0)
                    {
                        if (cachePolicy == CachePolicy.AllowCache)
                        {
                            result = _workflow.Execute(source, dest, symbol, CachePolicy.ForceRefresh);
                        }
                    }
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
                catch (Exception exception)
                {
                    Console.WriteLine($"An exception was thrown for {symbol}.");
                    Console.WriteLine(exception);
                    exceptions.Add(exception);
                }
            }

            if (!wereAnyProfitsFound) { Console.WriteLine("No profits found."); }
        }
    }
}
