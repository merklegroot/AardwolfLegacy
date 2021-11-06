using cache_lib.Models;
using dump_lib;
using idex_data_lib;
using idex_integration_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using trade_model;
using web_util;
using config_client_lib;
using idex_client_lib;

namespace idex_integration_lib_integration_tests
{
    [TestClass]
    public class IdexIntegrationTests
    {
        private IdexIntegration _integration;

        public IdexIntegrationTests()
        {
            var webUtil = new WebUtil();
            var configClient = new ConfigClient();

            var holdingsRepo = new IdexHoldingsRepo(configClient);
            var orderBookRepo = new IdexOrderBookRepo(configClient);
            var openOrdersRepo = new IdexOpenOrdersRepo(configClient);
            var historyRepo = new IdexHistoryRepo(configClient);
            var idexClient = new IdexClient(webUtil);
            var log = new Mock<ILogRepo>();

            _integration = new IdexIntegration(
                webUtil, 
                configClient,
                holdingsRepo,                
                orderBookRepo,
                openOrdersRepo,
                historyRepo,
                idexClient,
                log.Object);
        }

        [TestMethod]
        public void Idex__get_trading_pairs()
        {
            var tradingPairs = _integration.GetTradingPairs();
            tradingPairs.Dump();
        }

        [TestMethod]
        public void Idex__get_enj_order_book()
        {
            var results = _integration.GetOrderBook(new TradingPair("ENJ", "ETH"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Idex__get_idh_order_book()
        {
            var results = _integration.GetOrderBook(new TradingPair("IDH", "ETH"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Idex__get_idh_order_book__allow_cache()
        {
            var results = _integration.GetOrderBook(new TradingPair("IDH", "ETH"), CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Idex__get_ship_order_book()
        {
            var results = _integration.GetOrderBook(new TradingPair("SHIP", "ETH"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Idex__get_ecom_order_book()
        {
            var results = _integration.GetOrderBook(new TradingPair("ECOM", "ETH"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Idex__get_stp_order_book()
        {
            var results = _integration.GetOrderBook(new TradingPair("STP", "ETH"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Idex__get_loom_order_book()
        {
            var results = _integration.GetOrderBook(new TradingPair("LOOM", "ETH"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Idex__get_trx_order_book()
        {
            var results = _integration.GetOrderBook(new TradingPair("TRX", "ETH"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Idex__get_mod_order_book()
        {
            var results = _integration.GetOrderBook(new TradingPair("MOD", "ETH"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Idex__get_man_order_book()
        {
            var results = _integration.GetOrderBook(new TradingPair("MAN", "ETH"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Idex__relayed_order_book_contents()
        {
            var results = _integration.RequestRelayedNativeOrderBook("ENJ", "ETH");
            results.Dump();
        }

        [TestMethod]
        public void Idex__GetNativeUserTradeHistory()
        {
            var results = _integration.RequestUserTradeHistoryFromApi();
            results.Dump();
        }

        [TestMethod]
        public void Idex__get_ticker()
        {
            var results = _integration.GetTicker(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Idex__get_ticker_for_trading_pair()
        {
            var results = _integration.GetTicker(new TradingPair("ENJ", "ETH"));
            results.Dump();
        }

        [TestMethod]
        public void Idex__get_commodities()
        {
            var results = _integration.GetCommodities(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Idex__get_deposit_addresses()
        {
            var results = _integration.GetDepositAddresses(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Idex__get_user_trade_history__force_refresh()
        {
            var results = _integration.GetUserTradeHistory(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Idex__get_user_trade_history_from_api__force_refresh()
        {
            var results = _integration.GetUserTradeHistoryFromApi(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Idex__get_user_trade_history_from_api__only_use_cache_unless_empty()
        {
            var results = _integration.GetUserTradeHistoryFromApi(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Idex__get_open_orders_from_api()
        {
            var results = _integration.GetNativeOpenOrders(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Idex__get_open_orders()
        {
            var results = _integration.GetOpenOrders(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Idex__get_holdings()
        {
            var results = _integration.GetHoldings(CachePolicy.ForceRefresh);
            results.Dump();
        }
    }
}
