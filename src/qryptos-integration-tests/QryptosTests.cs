using cache_lib.Models;
using dump_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using qryptos_lib;
using Shouldly;
using System;
using System.Linq;
using trade_model;
using trade_node_integration;
using trade_res;
using web_util;
using config_client_lib;
using exchange_client_lib;
using trade_lib.Repo;
using qryptos_lib.Client;
using System.Collections.Generic;

namespace qryptos_integration_tests
{
    [TestClass]
    public class QryptosTests
    {
        private IExchangeClient _exchangeClient;
        private QryptosIntegration _qryptos;

        [TestInitialize]
        public void Setup()
        {
            var configClient = new ConfigClient();
            var qryptosClient = new QryptosClient();

            var webUtil = new WebUtil();
            var log = new Mock<ILogRepo>();
            var openOrdersSnapshotRepo = new OpenOrdersSnapshotRepo();

            _exchangeClient = new ExchangeClient();
            
            _qryptos = new QryptosIntegration(configClient, qryptosClient, webUtil, openOrdersSnapshotRepo, log.Object);
        }

        [TestMethod]
        public void Qryptos__get_commodities__only_use_cache_unless_empty()
        {
            var results = _qryptos.GetCommodities(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_commodities__force_refresh()
        {
            var results = _qryptos.GetCommodities(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_commodities__q()
        {
            var results = _qryptos.GetCommodities(CachePolicy.ForceRefresh);

            results.Where(item => item.Symbol.ToUpper().StartsWith("Q"))
                .ToList().Dump();
        }

        [TestMethod]
        public void Qryptos__get_commodity__ven()
        {
            var results = _qryptos.GetCommodities(CachePolicy.ForceRefresh);
            results.Where(item => string.Equals(item.Symbol, "VEN", StringComparison.InvariantCultureIgnoreCase))
                .ToList()
                .Dump();
        }

        [TestMethod]
        public void Qryptos__get_commodity__ont()
        {
            var results = _qryptos.GetCommodities(CachePolicy.ForceRefresh);
            results.Where(item => string.Equals(item.Symbol, "ONT", StringComparison.InvariantCultureIgnoreCase))
                .ToList()
                .Dump();
        }

        [TestMethod]
        public void Qryptos__get_commodity__ong()
        {
            var results = _qryptos.GetCommodities(CachePolicy.ForceRefresh);
            results.Where(item => string.Equals(item.Symbol, "ONG", StringComparison.InvariantCultureIgnoreCase))
                .ToList()
                .Dump();
        }

        [TestMethod]
        public void Qryptos__get_native_products__only_use_cache_unless_empty()
        {
            var results = _qryptos.GetNativeProducts(CachePolicy.OnlyUseCacheUnlessEmpty);

            // results.Where(item => item.currency_pair_code.ToUpper() == "MITXETH").ToList().Dump() ;
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_native_products__only_use_cache_()
        {
            var results = _qryptos.GetNativeProducts(CachePolicy.OnlyUseCache);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_vet__only_use_cache_unless_empty()
        {
            var products = _qryptos.GetNativeProducts(CachePolicy.OnlyUseCacheUnlessEmpty);
            var matches = products.Data.Where(queryProduct => string.Equals("VET", queryProduct.base_currency, StringComparison.InvariantCultureIgnoreCase)).ToList();
            matches.Dump();
        }

        [TestMethod]
        public void Qryptos__get_trading_pairs__force_refresh()
        {
            var results = _qryptos.GetTradingPairs(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_trading_pairs__only_use_cache_unless_empty()
        {
            var results = _qryptos.GetTradingPairs(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_trading_pairs__allow_cache__ont()
        {
            var results = _qryptos.GetTradingPairs(CachePolicy.AllowCache);
            var ontResults = results.Where(item => string.Equals(item.Symbol, "ONT", StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            ontResults.Dump();     
        }

        [TestMethod]
        public void Qryptos__get_trading_pairs__allow_cache__can()
        {
            var results = _qryptos.GetTradingPairs(CachePolicy.AllowCache);
            var ontResults = results.Where(item => string.Equals(item.Symbol, "CAN", StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            ontResults.Dump();
        }

        [TestMethod]
        public void Qryptos__get_trading_pairs__allow_cache()
        {
            var results = _qryptos.GetTradingPairs(CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_trading_pairs__force_refresh__ven()
        {
            var results = _qryptos.GetTradingPairs(CachePolicy.ForceRefresh);
            var matches = results.Where(item => string.Equals(item.Symbol, "VEN", StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            matches.Dump();
        }

        [TestMethod]
        public void Qryptos__get_trading_pairs__force_refresh__ont()
        {
            var results = _qryptos.GetTradingPairs(CachePolicy.ForceRefresh);
            var matches = results.Where(item => string.Equals(item.Symbol, "ONT", StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            matches.Dump();
        }

        [TestMethod]
        public void Qryptos__get_order_book__ont_eth__force_refresh()
        {
            var results = _qryptos.GetOrderBook(new TradingPair("ONT", "ETH"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_order_book__vzt_eth__force_refresh()
        {
            var results = _qryptos.GetOrderBook(new TradingPair("VZT", "ETH"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_order_book__stu_eth__force_refresh()
        {
            var results = _qryptos.GetOrderBook(new TradingPair("STU", "ETH"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_order_book__ven_eth__force_refresh()
        {
            var results = _qryptos.GetOrderBook(new TradingPair("VEN", "ETH"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_order_book__enj_btc__force_refresh()
        {
            var results = _qryptos.GetOrderBook(new TradingPair("ENJ", "BTC"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_order_book__mitx_eth__force_refresh()
        {
            var results = _qryptos.GetOrderBook(new TradingPair("MITX", "ETH"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_order_book__alx_eth__only_use_cache()
        {
            var results = _qryptos.GetOrderBook(new TradingPair("ALX", "ETH"), CachePolicy.OnlyUseCache);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_order_book__lnd_eth__allow_cache()
        {
            var results = _qryptos.GetOrderBook(new TradingPair("LND", "ETH"), CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_order_book__alx_eth__only_use_cache_unless_empty()
        {
            var results = _qryptos.GetOrderBook(new TradingPair("ALX", "ETH"), CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_order_book__alx_eth__allow_cache()
        {
            var results = _qryptos.GetOrderBook(new TradingPair("ALX", "ETH"), CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_order_book__alx_eth__force_refresh()
        {
            var results = _qryptos.GetOrderBook(new TradingPair("ALX", "ETH"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_order_book__can_btc__force_refresh()
        {
            var results = _qryptos.GetOrderBook(new TradingPair("CAN", "BTC"), CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_adh_deposit_address__only_use_cache_unless_empty()
        {
            var results = _qryptos.GetDepositAddress("adh", CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_holdings__allow_cache()
        {
            var results = _qryptos.GetHoldings(CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_holdings__force_refresh()
        {
            var results = _qryptos.GetHoldings(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_holdings__qash__force_refresh()
        {
            var results = _qryptos.GetBalanceForSymbol("QASH", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_holdings__ETH__force_refresh()
        {
            var results = _qryptos.GetBalanceForSymbol("ETH", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_holdings__ukg__force_refresh()
        {
            var results = _qryptos.GetBalanceForSymbol("UKG", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_vzt_holdings()
        {
            var commodity = CommodityRes.Vezt;

            var holdings = _qryptos.GetHoldings(CachePolicy.ForceRefresh);
            var available = holdings.GetAvailableForSymbol(commodity.Symbol);

            available.Dump();
        }

        [TestMethod]
        public void Qryptos__get_cached_order_books()
        {
            var results = _qryptos.GetCachedOrderBooks();
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_crypto_accounts()
        {
            var results = _qryptos.GetNativeCryptoAccounts(CachePolicy.ForceRefresh);
            results.Dump();
        }
        
        [TestMethod]
        public void Qryptos__get_trading_accounts()
        {
            var results = _qryptos.GetNativeTradingAccounts(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_native_open_orders__dent_qash__allow_cache()
        {
            var results = _qryptos.GetNativeOpenOrders("DENT", "QASH", CachePolicy.AllowCache);
            results.Dump();
        }
        
        [TestMethod]
        public void Qryptos__get_native_open_orders__mitx_eth__force_refresh()
        {
            var results = _qryptos.GetNativeOpenOrders("MITX", "ETH", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_open_orders_v2()
        {
            var openOrders = _qryptos.GetOpenOrdersV2();
            openOrders.Where(item => item.OpenOrders != null && item.OpenOrders.Any())
                .ToList()
                .Dump();
        }

        [TestMethod]
        public void Qryptos__get_open_orders__mitx_qash__force_refresh()
        {
            var results = _qryptos.GetOpenOrders("MITX", "QASH", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_open_orders__mrk_qash__force_refresh()
        {
            var results = _qryptos.GetOpenOrders("MRK", "QASH", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_open_orders__dent_eth__force_refresh()
        {
            var results = _qryptos.GetOpenOrders("DENT", "ETH", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_open_orders__can_btc__force_refresh()
        {
            var results = _qryptos.GetOpenOrders("CAN", "BTC", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_open_orders__can_eth__force_refresh()
        {
            var results = _qryptos.GetOpenOrders("CAN", "ETH", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_open_orders__can_qash__force_refresh()
        {
            var results = _qryptos.GetOpenOrders("CAN", "QASH", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_open_orders__mrk_qash__only_use_cache_unless_empty()
        {
            var results = _qryptos.GetOpenOrders("MRK", "QASH", CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__get_open_orders__mitx_eth__force_refresh()
        {
            var results = _qryptos.GetOpenOrders("MITX", "ETH", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Qryptos__cancel_order()
        {
            const string NativeOrderId = "477958084";
            _qryptos.CancelOrder(NativeOrderId);
        }

        [TestMethod]
        public void Qryptos__cancel_all_open_orders()
        {
            var exceptions = new List<Exception>();

            var openOrders = _qryptos.GetOpenOrdersV2();
            foreach (var group in openOrders)
            {
                foreach (var openOrder in group.OpenOrders)
                {
                    try
                    {
                        _qryptos.CancelOrder(openOrder.OrderId);
                    }
                    catch(Exception exception)
                    {
                        exceptions.Add(exception);
                    }
                }
            }

            if (exceptions.Any())
            {
                throw new AggregateException(exceptions);
            }
        }

        [TestMethod]
        public void Qryptos__buy_limit__mitx_qash()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string Symbol = "MITX";
            const string BaseSymbol = "QASH";
            var quantity = 100;
            var price = 0.00342m;
            var tradingPair = new TradingPair(Symbol, BaseSymbol);
            var result = _qryptos.BuyLimit(tradingPair, quantity, price);

            result.Dump();
        }

        [TestMethod]
        public void Qryptos__buy_limit__stx_btc()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }
            // ExchangeClient-- Buy Limit on Qryptos for 
            // 5664.1951142398744181152526077 CAN at 0.0000070465 BTC.

            const string Symbol = "STX";
            const string BaseSymbol = "BTC";
            const decimal quantity = 24.0m;
            const decimal price = 0.00000587m;

            var result = _qryptos.BuyLimit(new TradingPair(Symbol, BaseSymbol), quantity, price);
            result.Dump();
        }

        [TestMethod]
        public void Qryptos__buy_limit__mith_btc()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string Symbol = "MITH";
            const string BaseSymbol = "BTC";
            const decimal quantity = 12.54571165m;
            const decimal price = 0.0000211m;

            var result = _qryptos.BuyLimit(new TradingPair(Symbol, BaseSymbol), quantity, price);
            result.Dump();
        }

        [TestMethod]
        public void Qryptos__buy_limit__can_btc()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }
            // ExchangeClient-- Buy Limit on Qryptos for 
            // 5664.1951142398744181152526077 CAN at 0.0000070465 BTC.

            const string Symbol = "CAN";
            const string BaseSymbol = "BTC";
            const decimal quantity = 5664.1951142398744181152526077m;
            const decimal price = 0.0000070465m;

            var result = _qryptos.BuyLimit(new TradingPair(Symbol, BaseSymbol), quantity, price);
            result.Dump();
        }

        [TestMethod]
        public void Qryptos__sell_limit__vet_eth()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string Symbol = "VET";
            const string BaseSymbol = "ETH";
            var quantity = 574.30481344686553134468655315m;
            var price = 0.001929807000m;
            var tradingPair = new TradingPair(Symbol, BaseSymbol);
            var result = _qryptos.SellLimit(tradingPair, quantity, price);

            result.Dump();
        }
    }
}
