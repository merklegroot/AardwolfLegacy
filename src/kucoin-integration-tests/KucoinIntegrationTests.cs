using cache_lib.Models;
using cryptocompare_lib;
using dump_lib;
using env_config_lib;
using config_client_lib;
using kucoin_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using rabbit_lib;
using System;
using System.Linq;
using trade_email_lib;
using trade_model;
using trade_node_integration;
using wait_for_it_lib;
using web_util;
using System.Diagnostics;
using exchange_client_lib;
using kucoin_lib.Client;

namespace kucoin_integration_tests
{
    [TestClass]
    public class KucoinIntegrationTests
    {
        private static bool ShouldCommit = false;

        private KucoinIntegration _kucoin;
        private IExchangeClient _exchangeClient;
        private CryptoCompareIntegration _cryptoCompare;

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            var waitForIt = new WaitForIt();
            var log = new Mock<ILogRepo>();
            var configClient = new ConfigClient();

            var tradeNodeUtil = new TradeNodeUtil(configClient, webUtil, log.Object);
            var tradeEmailUtil = new TradeEmailUtil(webUtil);

            _cryptoCompare = new CryptoCompareIntegration(webUtil, configClient);

            var envConfig = new EnvironmentConfigRepo();
            var rabbitConnectionFactory = new RabbitConnectionFactory(envConfig);

            _exchangeClient = new ExchangeClient();
            var kucoinClient = new KucoinClient(webUtil);

            _kucoin = new KucoinIntegration(
                kucoinClient,
                tradeNodeUtil,
                tradeEmailUtil,
                webUtil,
                configClient,
                waitForIt,
                rabbitConnectionFactory,
                log.Object);
        }

        private T Time<T>(Func<T> method, string desc)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                return method();
            }
            finally
            {
                stopWatch.Stop();
                Console.WriteLine($"{desc} -- {stopWatch.ElapsedMilliseconds} ms");
            }
        }

        [TestMethod]
        public void Kucoin__get_order_book__kcs_btc()
        {
            var result = _kucoin.GetOrderBook(new TradingPair("KCS", "BTC"), CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Kucoin__get_order_book__eos_eth__force_refresh()
        {
            var result = _kucoin.GetOrderBook(new TradingPair("EOS", "ETH"), CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Kucoin__get_order_book__eth_btc__force_refresh()
        {
            var result = _kucoin.GetOrderBook(new TradingPair("ETH", "BTC"), CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Kucoin__get_order_book__eos_eth__only_use_cache_unless_empty()
        {
            var result = _kucoin.GetOrderBook(new TradingPair("EOS", "ETH"), CachePolicy.OnlyUseCacheUnlessEmpty);
            result.Dump();
        }

        [TestMethod]
        public void Kucoin__get_order_book__bnt_eth__allow_cache()
        {
            var result = _kucoin.GetOrderBook(new TradingPair("BNT", "ETH"), CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Kucoin__get_order_book__lsk_btc__allow_cache()
        {
            var result = _kucoin.GetOrderBook(new TradingPair("LSK", "BTC"), CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Kucoin__get_order_book__lsk_btc__only_use_cache_unless_empty()
        {
            var result = _kucoin.GetOrderBook(new TradingPair("LSK", "BTC"), CachePolicy.OnlyUseCacheUnlessEmpty);
            result.Dump();
        }

        [TestMethod]
        public void Kucoin__get_order_book__cs_eth__force_refresh()
        {
            var result = _kucoin.GetOrderBook(new TradingPair("CS", "ETH"), CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Kucoin__get_order_book__opq_eth__force_refresh()
        {
            var result = _kucoin.GetOrderBook(new TradingPair("OPQ", "ETH"), CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Kucoin__get_order_book__cs_eth__only_use_cache_unless_empty()
        {
            var result = _kucoin.GetOrderBook(new TradingPair("CS", "ETH"), CachePolicy.OnlyUseCacheUnlessEmpty);
            result.Dump();
        }

        [TestMethod]
        public void Kucoin__get_commodity__force_refresh__ltc()
        {
            var result = _kucoin.GetCommodities(CachePolicy.ForceRefresh);
            var match = result.Where(item => string.Equals(item.Symbol, "LTC", StringComparison.InvariantCultureIgnoreCase));
            match.Dump();
        }

        [TestMethod]
        public void Kucoin__get_commodities__force_refresh()
        {
            var result = _kucoin.GetCommodities(CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Kucoin__get_commodities__only_use_cache_unless_empty()
        {
            var result = _kucoin.GetCommodities(CachePolicy.OnlyUseCacheUnlessEmpty);
            result.Dump();
        }

        [TestMethod]
        public void Kucoin__get_trading_pairs__force_refresh()
        {
            var result = _kucoin.GetTradingPairs(CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Kucoin__get_trading_pairs__only_use_cache_unless_empty()
        {
            var result = Time(() => _kucoin.GetTradingPairs(CachePolicy.OnlyUseCacheUnlessEmpty), "Get trading pairs");
            result.Dump();
        }

        [TestMethod]
        public void Kucoin__get_withdrawal_fees__force_refresh()
        {
            _kucoin.GetWithdrawalFees(CachePolicy.ForceRefresh).Dump();
        }

        [TestMethod]
        public void Kucoin__get_withdrawal_fees__only_use_cache()
        {
            _kucoin.GetWithdrawalFees(CachePolicy.OnlyUseCache).Dump();
        }

        [TestMethod]
        public void Kucoin__get_withdrawal_fees__only_use_cache_unless_empty()
        {
            _kucoin.GetWithdrawalFees(CachePolicy.OnlyUseCacheUnlessEmpty).Dump();
        }

        [TestMethod]
        public void Kucoin__get_holdings__force_refresh()
        {
            _kucoin.GetHoldings(CachePolicy.ForceRefresh).Dump();
        }

        [TestMethod]
        public void Kucoin__get_holdings__only_use_cache()
        {
            var results = _kucoin.GetHoldings(CachePolicy.OnlyUseCache);
            results.Dump();
        }

        [TestMethod]
        public void Kucoin__get_holdings__only_use_cache_unless_empty()
        {
            var results = _kucoin.GetHoldings(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Kucoin__get_deposit_addresses_allow_cache()
        {
            var result = _kucoin.GetDepositAddresses(CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Kucoin__get_deposit_addresses_force_refresh()
        {
            var result = _kucoin.GetDepositAddresses(CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Kucoin__get_cached_order_books()
        {
            var results = _kucoin.GetCachedOrderBooks();
            results.Dump();
        }

        [TestMethod]
        public void Kucoin__get_open_orders__cs_eth__force_refresh()
        {
            var results = _kucoin.GetOpenOrders("CS", "ETH", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Kucoin__get_open_orders__usdc_eth__force_refresh()
        {
            var results = _kucoin.GetOpenOrdersForTradingPairV2("USDC", "ETH", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Kucoin__get_open_orders_v2()
        {
            var results = _kucoin.GetOpenOrdersV2();
            results.Dump();
        }
    }
}
