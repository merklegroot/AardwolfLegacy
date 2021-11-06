using cache_lib.Models;
using coss_data_lib;
using dump_lib;
using livecoin_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using trade_model;
using trade_node_integration;
using trade_res;
using web_util;
using config_client_lib;
using exchange_client_lib;
using livecoin_lib.Client;
using trade_constants;
using System.Linq;

namespace livecoin_integration_tests
{
    [TestClass]
    public class LivecoinIntegrationTests
    {
        private IExchangeClient _exchangeClient;
        private LivecoinIntegration _livecoin;

        [TestInitialize]
        public void Setup()
        {
            var configClient = new ConfigClient();
            _exchangeClient = new ExchangeClient();
            var webUtil = new WebUtil();
            var log = new Mock<ILogRepo>();

            var tradeNodeUtil = new TradeNodeUtil(configClient, webUtil, log.Object);

            var cossHistoryRepo = new CossHistoryRepo(configClient);
            var cossOpenOrderRepo = new CossOpenOrderRepo(configClient);
            var cossXhrOpenOrderRepo = new CossXhrOpenOrderRepo(configClient);

            var livecoinClient = new LivecoinClient(webUtil);

            _livecoin = new LivecoinIntegration(
                livecoinClient,
                tradeNodeUtil,
                webUtil,
                configClient,
                log.Object);
        }

        [TestMethod]
        public void Livecoin__get_trading_pairs__force_refresh()
        {
            var results = _livecoin.GetTradingPairs(CachePolicy.ForceRefresh);
            string.Join(Environment.NewLine, results).Dump();
        }

        [TestMethod]
        public void Livecoin__get_trading_pairs__allow_cache()
        {
            var results = _livecoin.GetTradingPairs(CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Livecoin__get_trading_pairs__only_use_cache_unless_empty()
        {
            var results = _livecoin.GetTradingPairs(CachePolicy.OnlyUseCacheUnlessEmpty);
            string.Join(Environment.NewLine, results).Dump();
        }

        [TestMethod]
        public void Livecoin__get_order_book__ETH_BTC__force_refresh()
        {
            var results = _livecoin.GetOrderBook(new TradingPair("ETH", "BTC"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Livecoin__get_order_book__CHSB_BTC__force_refresh()
        {
            var results = _livecoin.GetOrderBook(new TradingPair("CHSB", "BTC"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Livecoin__get_order_book__CHSB_BTC__only_use_cache_unless_empty()
        {
            var results = _livecoin.GetOrderBook(new TradingPair("CHSB", "BTC"), CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();

            results.ShouldNotBeNull();
            results.Asks.ShouldNotBeEmpty();
            results.Bids.ShouldNotBeEmpty();
        }

        [TestMethod]
        public void Livecoin__get_order_book__MTRc_BTC__only_use_cache_unless_empty()
        {
            var results = _livecoin.GetOrderBook(new TradingPair("MTRc", "BTC"), CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();            
        }

        [TestMethod]
        public void Livecoin__cached_order_books()
        {
            var results = _livecoin.GetCachedOrderBooks();
            results.Dump();
        }

        [TestMethod]
        public void Livecoin__get_withdrawal_fees()
        {
            var results = _livecoin.GetWithdrawalFees(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Livecoin__get_history__force_refresh()
        {
            var results = _livecoin.GetUserTradeHistory(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Livecoin__get_history__allow_cache()
        {
            var results = _livecoin.GetUserTradeHistory(CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Livecoin__get_history__only_use_cache_unless_empty()
        {
            var results = _livecoin.GetUserTradeHistory(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Livecoin__get_holdings__force_refresh()
        {
            _livecoin.GetHoldings(CachePolicy.ForceRefresh).Dump();
        }

        [TestMethod]
        public void Livecoin__get_holdings__allow_cache()
        {
            _livecoin.GetHoldings(CachePolicy.AllowCache).Dump();
        }

        [TestMethod]
        public void Livecoin__get_deposit_addresses__allow_cache()
        {
            var result = _livecoin.GetDepositAddresses(CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Livecoin__get_eth_deposit_address()
        {
            var result = _livecoin.GetDepositAddress("ETH", CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Livecoin__TryToGetDepositAddressesFromTheApi()
        {
            _livecoin.TryToGetDepositAddressesFromTheApi();
        }

        [TestMethod]
        public void Livecoin__get_commodities__force_refresh()
        {
            var results = _livecoin.GetCommodities(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Livecoin__get_commodities__allow_cache()
        {
            var results = _livecoin.GetCommodities(CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Livecoin__get_commodities__only_use_cache_unless_empty()
        {
            var results = _livecoin.GetCommodities(CachePolicy.OnlyUseCache);
            results.Dump();
        }

        [TestMethod]
        public void Livecoin__get_commodities__only_use_cache()
        {
            var results = _livecoin.GetCommodities(CachePolicy.OnlyUseCache);
            results.Dump();
        }

        [TestMethod]
        public void Livecoin__send_ark_to_coss()
        {
            // var results = _integration.
            var commodity = CommodityRes.Ark;
            var depositAddress = _exchangeClient.GetDepositAddress(IntegrationNameRes.Coss, commodity.Symbol, CachePolicy.AllowCache);

            const decimal QuantityToWithdraw = 69.5136m;
            var result = _livecoin.Withdraw(commodity, QuantityToWithdraw, depositAddress);

            result.Dump();
        }

        [TestMethod]
        public void Livecoin__native_client_test()
        {
            var results = _livecoin.NativeClientTest();
            results.Dump();
        }

        [TestMethod]
        public void Livecoin__get_open_orders__force_refresh()
        {
            var results = _livecoin.GetOpenOrders(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Livecoin__get_open_orders__only_use_cache_unless_empty()
        {
            var results = _livecoin.GetOpenOrders(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Livecoin__cancel_all_open_orders()
        {
            var openOrders = _livecoin.GetOpenOrders(CachePolicy.ForceRefresh);
            if (openOrders == null || !openOrders.Any())
            {
                Assert.Inconclusive("There are no open orders to cancel.");
            }

            foreach (var openOrder in openOrders ?? new List<OpenOrderForTradingPair>())
            {
                Console.WriteLine($"Cancelling open order {openOrder.OrderId}.");
                _livecoin.CancelOrder(openOrder.OrderId);
            }

            var openOrdersAfterCancelling = _livecoin.GetOpenOrders(CachePolicy.ForceRefresh);
            (openOrdersAfterCancelling == null || !openOrdersAfterCancelling.Any()).ShouldBe(true);
        }

        [TestMethod]
        public void Livecoin__buy_limit__rep_eth()
        {
            const string Symbol = "REP";
            const string BaseSymbol = "ETH";
            const decimal Quantity = 0.1m;
            const decimal Price = 0.052m;

            var results = _livecoin.BuyLimit(new TradingPair(Symbol, BaseSymbol), Quantity, Price);           

            results.Dump();
        }
    }
}
