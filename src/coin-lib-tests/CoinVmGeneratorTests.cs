using System;
using System.Collections.Generic;
using System.Linq;
using coin_lib;
using coin_lib.ViewModel;
using dump_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using trade_contracts;
using cache_lib.Models;
using wait_for_it_lib;
using web_util;
using exchange_client_lib;
using trade_res;

namespace coin_lib_tests
{
    [TestClass]
    public class CoinVmGeneratorTests
    {
        private CoinVmGenerator _coinVmGenerator;
        private IExchangeClient _exchangeClient;

        private Mock<ILogRepo> _log;

        [TestInitialize]
        public void Setup()
        {
            var waitForIt = new WaitForIt();
            var webUtil = new WebUtil();

            _log = new Mock<ILogRepo>();

            _exchangeClient = new ExchangeClient();           
        }

        [TestMethod]
        public void Coin_vm_generator__time_the_constructor()
        {
            _coinVmGenerator = new CoinVmGenerator(_exchangeClient, _log.Object);
            var qryptos = _coinVmGenerator._exchanges.Single(item => string.Equals(item.Exchange, "qryptos", StringComparison.InvariantCultureIgnoreCase));

            var ven = qryptos.TradingPairs.Where(tp => tp.Symbol.ToUpper().StartsWith("VE")).ToList();
            ven.Dump();
        }

        [TestMethod]
        public void Coin_vm_generator__livecoin__kucoin_ven_eth__allow_cache()
        {
            _coinVmGenerator = new CoinVmGenerator(_exchangeClient, _log.Object);

            var cachePolicy = CachePolicy.AllowCache;

            var tradingPair = new TradingPairContract("VEN", "ETH");
            
            var exchanges = new List<string> { "kucoin", "livecoin" }.Select(queryIntegration =>
            {
                return new ExchangeContainer
                {
                    Exchange = "kucoin",
                    Commodities = _exchangeClient.GetCommoditiesForExchange(queryIntegration, cachePolicy),
                    TradingPairs = _exchangeClient.GetTradingPairs(queryIntegration, cachePolicy),
                    WithdrawalFees = _exchangeClient.GetWithdrawalFees(queryIntegration, cachePolicy)
                };
            }).ToList();

            //exchanges.ForEach(exchange =>
            //{
            //    exchange.CommoditiesTask.Wait();
            //    exchange.TradingPairsTask.Wait();
            //    exchange.WithdrawalFeesTask.Wait();
            //});

            var results = _coinVmGenerator.GenerateVm(tradingPair.Symbol, tradingPair.BaseSymbol, exchanges, CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Coin_vm_generator__hitbtc_binance__ltc_btc__allow_cache()
        {
            _coinVmGenerator = new CoinVmGenerator(_exchangeClient, _log.Object);

            var cachePolicy = CachePolicy.AllowCache;

            var tradingPair = new TradingPairContract("LTC", "BTC");

            var exchanges = new List<string> { "hitbtc", "binance" }.Select(queryExchange =>
            {
                return new ExchangeContainer
                {
                    Exchange = "hitbtc",
                    Commodities = _exchangeClient.GetCommoditiesForExchange(queryExchange, cachePolicy),
                    TradingPairs = _exchangeClient.GetTradingPairs(queryExchange, cachePolicy),
                    WithdrawalFees = _exchangeClient.GetWithdrawalFees(queryExchange, cachePolicy),
                };
            }).ToList();

            var results = _coinVmGenerator.GenerateVm(tradingPair.Symbol, tradingPair.BaseSymbol, exchanges, CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Coin_vm_generator__get_trading_pairs_with_exchanges()
        {
            _coinVmGenerator = new CoinVmGenerator(_exchangeClient, _log.Object);
            var results = _coinVmGenerator.GetTradingPairsWithExchanges();
            results.Dump();
        }

        [TestMethod]
        public void Coin_vm_generator__compare_binance_VET_to_qryptos_VET()
        {            
            var generateContainer = new Func<string, ExchangeContainer>(queryExchange =>
            {
                return new ExchangeContainer
                {
                    Exchange = queryExchange,
                    Commodities = _exchangeClient.GetCommoditiesForExchange(queryExchange, CachePolicy.OnlyUseCache),
                    TradingPairs = _exchangeClient.GetTradingPairs(queryExchange, CachePolicy.OnlyUseCache),
                    WithdrawalFees = _exchangeClient.GetWithdrawalFees(queryExchange, CachePolicy.OnlyUseCache)
                };
            });

            var exchangeA = generateContainer("binance");
            var pairA = exchangeA.TradingPairs.SingleOrDefault(item =>
                string.Equals(item.Symbol, "VET", StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.BaseSymbol, "ETH", StringComparison.InvariantCultureIgnoreCase));

            var exchangeB = generateContainer("qryptos");
            var pairB = exchangeB.TradingPairs.SingleOrDefault(item =>
                string.Equals(item.NativeSymbol, "VET", StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.BaseSymbol, "ETH", StringComparison.InvariantCultureIgnoreCase));

            var doTheyMatch = CoinVmGenerator.DoTradingPairsMatch(pairA, pairB);
            doTheyMatch.Dump();
        }

        [TestMethod]
        public void Coin_vm_generator__get_all_orders__only_use_cache()
        {
            _coinVmGenerator = new CoinVmGenerator(_exchangeClient, _log.Object);
            var result = _coinVmGenerator.GetAllOrders(null, null, CachePolicy.OnlyUseCache);
            result.Dump();
        }

        [TestMethod]
        public void Coin_vm_generator__get_all_orders__binance_vs_qryptos__only_use_cache()
        {
            var qryptosTradingPairs = _exchangeClient.GetTradingPairs("qryptos", CachePolicy.OnlyUseCacheUnlessEmpty);
            var qryptosVen = qryptosTradingPairs.Where(item => item.Symbol.ToUpper().StartsWith("VE")).ToList();

            _coinVmGenerator = new CoinVmGenerator(_exchangeClient, _log.Object);
            var exchangesToInclude = new List<string> { "binance", "qryptos" };

            var result = _coinVmGenerator.GetAllOrders(null, exchangesToInclude, CachePolicy.OnlyUseCache);

            var venMatches = result.Coins.Where(item => item.Symbol.ToUpper().StartsWith("VE")).ToList();
            //venMatches.Dump();

            result.Dump();
        }

        [TestMethod]
        public void Coin_vm_generator__binance_kucoin__eth_btc()
        {
            _coinVmGenerator = new CoinVmGenerator(_exchangeClient, _log.Object);
            var result = _coinVmGenerator.GetOrdersInternal("ETH", "BTC", new List<string> { ExchangeNameRes.KuCoin, ExchangeNameRes.Binance }, CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Coin_vm_generator__coss_kucoin__lsk_btc()
        {
            _coinVmGenerator = new CoinVmGenerator(_exchangeClient, _log.Object);
            var result = _coinVmGenerator.GetOrdersInternal("LSK", "BTC", new List<string> { ExchangeNameRes.Coss, ExchangeNameRes.KuCoin }, CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Coin_vm_generator__coss_qryptos__lala_eth__only_use_cache_unless_empty()
        {
            _coinVmGenerator = new CoinVmGenerator(_exchangeClient, _log.Object);
            var result = _coinVmGenerator.GetOrdersInternal("LALA", "ETH", new List<string> { ExchangeNameRes.Coss, ExchangeNameRes.Qryptos }, CachePolicy.OnlyUseCacheUnlessEmpty);
            result.Dump();
        }
    }
}
