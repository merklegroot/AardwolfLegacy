using cache_lib.Models;
using dump_lib;
using exchange_service_con;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using trade_contracts;
using trade_res;
using exchange_client_lib;
using System.Collections.Generic;
using trade_constants;
using System.IO;
using Newtonsoft.Json;

namespace exchange_client_tests
{
    [TestClass]
    public class ExchangeClientTests
    {
        // private static bool UseTestQueue = true;
        private static bool UseTestQueue = false;

        private const string ExchangeTestQueue = "ExchangeTestQueue";

        private ExchangeClient _client;

        [TestInitialize]
        public void Setup()
        {
            _client = new ExchangeClient();
            if (UseTestQueue)
            {
                _client.OverrideQueue(ExchangeTestQueue);
                _client.OverrideTimeout(TimeSpan.FromMinutes(10));
                StartProgram();
            }
        }

        private void StartProgram()
        {
            var slim = new ManualResetEventSlim(false);

            var runner = new ExchangeServiceRunner();
            runner.OnStarted += () => { slim.Set(); };
            var task = new Task(() =>
            {
                runner.Run(ExchangeTestQueue);
            }, TaskCreationOptions.LongRunning);
            task.Start();

            slim.Wait();
        }

        [TestMethod]
        public void Exchange_client__get_exchanges()
        {
            var exchanges = _client.GetExchanges();
            exchanges.Dump();

            exchanges.ShouldNotBeNull();
            exchanges.ShouldNotBeEmpty();
        }

        [TestMethod]
        public void Exchange_client__get_cryptocompare_symbols()
        {
            var symbols = _client.GetCryptoCompareSymbols();
            symbols.Dump();

            symbols.ShouldNotBeNull();
            symbols.ShouldNotBeEmpty();
        }

        [TestMethod]
        public void Exchange_client__get_trading_pairs__coss__force_refresh()
        {
            var tradingPairs = _client.GetTradingPairs(IntegrationNameRes.Coss, CachePolicy.ForceRefresh);
            tradingPairs.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_trading_pairs__coss__only_use_cache_unless_empty()
        {
            var tradingPairs = _client.GetTradingPairs(IntegrationNameRes.Coss, CachePolicy.OnlyUseCacheUnlessEmpty);
            tradingPairs.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_trading_pairs__coss__allow_cache()
        {
            var tradingPairs = _client.GetTradingPairs(IntegrationNameRes.Coss, CachePolicy.AllowCache);
            tradingPairs.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_trading_pairs__yobit()
        {
            var tradingPairs = _client.GetTradingPairs("yobit", CachePolicy.OnlyUseCacheUnlessEmpty);
            tradingPairs.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_trading_pairs__qryptos__allow_cache()
        {
            var tradingPairs = _client.GetTradingPairs("qryptos", CachePolicy.AllowCache);
            tradingPairs.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_trading_pairs__qryptos__only_use_cache_unless_empty()
        {
            var tradingPairs = _client.GetTradingPairs("qryptos", CachePolicy.OnlyUseCacheUnlessEmpty);
            tradingPairs.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_trading_pairs__qryptos__ven_or_vet()
        {
            var tradingPairs = _client.GetTradingPairs("qryptos", CachePolicy.OnlyUseCacheUnlessEmpty);
            var venVet = tradingPairs.Where(item => item.Symbol.ToUpper().StartsWith("VE"));

            venVet.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_trading_pairs__hitbtc__only_use_cache_unless_empty()
        {
            var tradingPairs = Time(() => _client.GetTradingPairs("hitbtc", CachePolicy.OnlyUseCacheUnlessEmpty), "Get HitBtc trading pairs - only use cache unless empty.");
            tradingPairs.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_trading_pairs__hitbtc__force_refresh()
        {
            var tradingPairs = Time(() => _client.GetTradingPairs("hitbtc", CachePolicy.ForceRefresh), "Get HitBtc trading pairs - force refresh.");
            tradingPairs.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_trading_pairs__kraken()
        {
            var tradingPairs = _client.GetTradingPairs("kraken", CachePolicy.OnlyUseCacheUnlessEmpty);
            tradingPairs.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_trading_pairs__bitz()
        {
            var tradingPairs = _client.GetTradingPairs("bitz", CachePolicy.OnlyUseCacheUnlessEmpty);
            tradingPairs.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_trading_pairs__binance()
        {
            var tradingPairs = _client.GetTradingPairs("binance", CachePolicy.OnlyUseCacheUnlessEmpty);

            tradingPairs.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_trading_pairs__kucoin__only_use_cache_unless_empty()
        {
            var tradingPairs = _client.GetTradingPairs("kucoin", CachePolicy.OnlyUseCacheUnlessEmpty);
            tradingPairs.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_trading_pairs__kucoin__force_refresh()
        {
            var tradingPairs = _client.GetTradingPairs("kucoin", CachePolicy.ForceRefresh);
            tradingPairs.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_trading_pairs__coinbase__force_refresh()
        {
            var tradingPairs = _client.GetTradingPairs("kucoin", CachePolicy.ForceRefresh);
            tradingPairs.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_trading_pairs__kucoin__allow_cache()
        {
            var tradingPairs = _client.GetTradingPairs("kucoin", CachePolicy.AllowCache);
            tradingPairs.Dump();
        }

        private void Time(Action action, string desc)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            action();
            stopWatch.Stop();

            Console.WriteLine($"{desc} - {stopWatch.ElapsedMilliseconds} ms");
        }

        private T Time<T>(Func<T> method, string desc)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var value = method();
            stopWatch.Stop();

            Console.WriteLine($"{desc} - {stopWatch.ElapsedMilliseconds} ms");

            return value;
        }

        [TestMethod]
        public void Exchange_client__commodities_for_exchange__bitz__only_use_cache()
        {
            var results = _client.GetCommoditiesForExchange("bitz", CachePolicy.OnlyUseCache);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__commodities_for_exchange__bitz__only_use_cache_unless_empty()
        {   
            var results = _client.GetCommoditiesForExchange("bitz", CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__commodities_for_exchange__hitbtc__force_refresh()
        {
            var results = _client.GetCommoditiesForExchange(IntegrationNameRes.HitBtc, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__commodities_for_exchange__coinbase__force_refresh()
        {
            var results = _client.GetCommoditiesForExchange(IntegrationNameRes.Coinbase, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__commodity_for_exchange__ark__bitz()
        {
            var results = _client.GetCommoditiyForExchange("bitz", "ark", "ark", CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__commodity_for_exchange__can__kucoin()
        {
            var results = _client.GetCommoditiyForExchange("kucoin", "can", null, CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__commodity_for_exchange__blz_cryptopia()
        {
            var results = _client.GetCommoditiyForExchange("cryptopia", "blz", "blz", CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__commodity_for_exchange__ark__binance()
        {
            var results = _client.GetCommoditiyForExchange("binance", "ark", "ark", CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__commodity_for_exchange__enj__coss()
        {
            var results = _client.GetCommoditiyForExchange("coss", null, "ENJ", CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__commodity_for_exchange__btc__coss()
        {
            var results = _client.GetCommoditiyForExchange("coss", null, "BTC", CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__commodity_for_exchange__vet_qryptos()
        {
            var results = _client.GetCommoditiyForExchange("qryptos", null, "vet", CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__commodity_for_exchange__sphtx_kucoin__allow_cache()
        {
            var results = _client.GetCommoditiyForExchange("kucoin", null, "sphtx", CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__commodity_for_exchange__fyn_hitbtc()
        {
            var results = _client.GetCommoditiyForExchange("hitbtc", null, "fyn", CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__commodity_for_exchange__get_hitbtc()
        {
            var results = _client.GetCommoditiyForExchange("hitbtc", null, "get", CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }


        [TestMethod]
        public void Exchange_client__get_trading_pairs__binance_then_qryptos()
        {
            _client.GetTradingPairs("binance", CachePolicy.OnlyUseCacheUnlessEmpty);
            var results = _client.GetTradingPairs("qryptos", CachePolicy.OnlyUseCacheUnlessEmpty);
            var ven = results.Where(item => item.Symbol.ToUpper().StartsWith("VE")).ToList();
            ven.Dump();
        }

        [TestMethod]
        public void Exchange_client__commodity_for_exchange__vet_binance()
        {
            var results = _client.GetCommoditiyForExchange("binance", null, "vet", CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_order_book__coss_enj_eth__only_use_cache_unless_empty()
        {
            var results = _client.GetOrderBook("coss", "ENJ", "ETH", CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_order_book__bitz__ark_btc__force_refresh()
        {
            var results = _client.GetOrderBook(IntegrationNameRes.Bitz, "ARK", "BTC", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_order_book__binance_eth_btc__only_use_cache_unless_empty()
        {
            var results = _client.GetOrderBook(IntegrationNameRes.Binance, CommodityRes.Eth.Symbol, CommodityRes.Bitcoin.Symbol, CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_order_book__kucoin_lala_eth__only_use_cache_unless_empty()
        {
            var results = _client.GetOrderBook(IntegrationNameRes.KuCoin, CommodityRes.LaLaWorld.Symbol, CommodityRes.Eth.Symbol, CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_order_book__kucoin_lala_btc__only_use_cache_unless_empty()
        {
            var results = _client.GetOrderBook(IntegrationNameRes.KuCoin, CommodityRes.LaLaWorld.Symbol, CommodityRes.Bitcoin.Symbol, CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_order_book__hitbtc__usdt_tusd__allow_cache ()
        {
            var results = _client.GetOrderBook(IntegrationNameRes.HitBtc, "USDT", "TUSD", CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_order_book__binance_eth_btc__allow_cache()
        {
            var results = _client.GetOrderBook(IntegrationNameRes.Binance, CommodityRes.Eth.Symbol, CommodityRes.Bitcoin.Symbol, CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__refresh_order_book()
        {
            var results = _client.RefreshOrderBook("coss", "ENJ", "ETH");
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_cached_order_books__kucoin()
        {
            var results = _client.GetCachedOrderBooks("kucoin");
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_cached_order_books__qryptos()
        {
            var results = _client.GetCachedOrderBooks("qryptos");
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_cached_order_books__binance()
        {
            var results = _client.GetCachedOrderBooks("binance");
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_cached_order_books__livecoin()
        {
            var results = _client.GetCachedOrderBooks("livecoin");
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_cached_order_books__coss()
        {
            var results = _client.GetCachedOrderBooks("coss");
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_deposit_address__binance__ark()
        {
            var results = _client.GetDepositAddress("binance", "ark", CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_exchange_name__bitz()
        {
            var results = _client.GetExchangeName(IntegrationNameRes.Bitz);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_aggregate_history()
        {
            var results = _client.GetAggregateHistory(null, CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();

            // var filtered = results.History.Where(item => (item.Exchange ?? string.Empty).ToUpper().Contains("MEW")).ToList();
        }

        [TestMethod]
        public void Exchange_client__get_history__coinbase__only_use_cache_unless_empty()
        {
            var results = _client.GetExchangeHistory(IntegrationNameRes.Coinbase, 10, CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_history__kraken__only_use_cache_unless_empty()
        {
            var results = _client.GetExchangeHistory(IntegrationNameRes.Kraken, 10, CachePolicy.OnlyUseCacheUnlessEmpty);           
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_history__binance__only_use_cache_unless_empty()
        {
            var results = _client.GetExchangeHistory("binance", 10, CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_history__hitbtc__force_refresh()
        {
            var results = _client.GetExchangeHistory("hitbtc", 0, CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_history__coss__only_use_cache_unless_empty()
        {
            const int Limit = 10;
            var results = _client.GetExchangeHistory(IntegrationNameRes.Coss, Limit, CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();

            results.History.Count.ShouldBeLessThanOrEqualTo(Limit);
        }

        [TestMethod]
        public void Exchange_client__get_history__mew__only_use_cache_unless_empty()
        {
            const int Limit = 10;
            var results = _client.GetExchangeHistory(IntegrationNameRes.Mew, Limit, CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();

            results.History.Count.ShouldBeLessThanOrEqualTo(Limit);
        }

        [TestMethod]
        public void Exchange_client__get_history__kucoin__only_use_cache_unless_empty()
        {
            const int Limit = 10;
            var results = _client.GetExchangeHistory(IntegrationNameRes.KuCoin, Limit, CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__binance__get_balances()
        {
            var results = _client.GetBalances("binance", CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__livecoin__get_balances()
        {
            var results = _client.GetBalances(IntegrationNameRes.Livecoin, CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__buy_limit_v2__coss__eth_btc()
        {
            var shouldRun = false;
            if (!shouldRun) { throw new ApplicationException("This test works with live funds and must be run manually."); }

            const string Symbol = "ETH";
            const string BaseSymbol = "BTC";
            const decimal Price = 0.02m;
            const decimal Quantity = 0.01m;

            var results = _client.BuyLimitV2(IntegrationNameRes.Coss, Symbol, BaseSymbol, new trade_model.QuantityAndPrice
            {
                Price = Price,
                Quantity = Quantity
            });

            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__sell_limit_v2__coss__eth_btc()
        {
            var shouldRun = false;
            if (!shouldRun) { throw new ApplicationException("This test works with live funds and must be run manually."); }

            const string Symbol = "ETH";
            const string BaseSymbol = "BTC";
            const decimal Price = 0.04m;
            const decimal Quantity = 0.01m;

            var results = _client.SellLimitV2(IntegrationNameRes.Coss, Symbol, BaseSymbol, new trade_model.QuantityAndPrice
            {
                Price = Price,
                Quantity = Quantity
            });

            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__sell_limit__blocktrade__eth_btc()
        {
            var shouldRun = false;
            if (!shouldRun) { throw new ApplicationException("This test works with live funds and must be run manually."); }

            const string Symbol = "ETH";
            const string BaseSymbol = "BTC";
            const decimal Price = 0.03817m;
            const decimal Quantity = 0.01m;

            var results = _client.SellLimitV2(IntegrationNameRes.Blocktrade, Symbol, BaseSymbol, new trade_model.QuantityAndPrice
            {
                Price = Price,
                Quantity = Quantity
            });

            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__sell_limit_v2__coss__dash_btc()
        {
            var shouldRun = false;
            if (!shouldRun) { throw new ApplicationException("This test works with live funds and must be run manually."); }

            const string Symbol = "DASH";
            const string BaseSymbol = "BTC";
            const decimal Quantity = 0.00017178m;
            const decimal Price = 0.02316952m;

            var results = _client.SellLimitV2(IntegrationNameRes.Coss, Symbol, BaseSymbol, new trade_model.QuantityAndPrice
            {
                Price = Price,
                Quantity = Quantity
            });

            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__qryptos__get_balances__eth_and_qash__force_refresh()
        {
            var symbols = new List<string> { "ETH", "QASH" };
            var results = _client.GetBalances(IntegrationNameRes.Qryptos, symbols, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__coss__get_balances__force_refresh()
        {
            var results = _client.GetBalances(IntegrationNameRes.Coss, CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_ven_balance()
        {
            var results = _client.GetBalance(
                "binance", "VEN", CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_withdrawal_fee__coss_sub__only_use_cache_unless_empty()
        {
            var results = _client.GetWithdrawalFee(
                IntegrationNameRes.Coss, CommodityRes.Substratum.Symbol, CachePolicy.OnlyUseCacheUnlessEmpty);

            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_commodity_for_exchange__ctx__livecoin__allow_cache()
        {
            var results = _client.GetCommoditiyForExchange(IntegrationNameRes.Livecoin, "CTX", null, CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_commodity_details__ark()
        {
            var result = _client.GetCommodityDetails("ARK", CachePolicy.OnlyUseCacheUnlessEmpty);
            result.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_commodity_details__bchabc()
        {
            var result = _client.GetCommodityDetails("BCHABC", CachePolicy.OnlyUseCacheUnlessEmpty);
            result.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_commodities__only_use_cache_unless_empty()
        {
            var result = _client.GetCommodities(CachePolicy.OnlyUseCacheUnlessEmpty);
            result.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_commodities__allow_cache()
        {
            var result = _client.GetCommodities(CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_exchanges_for_commodity()
        {
            var result = _client.GetExchangesForCommodity("ARK", CachePolicyContract.OnlyUseCacheUnlessEmpty);
            result.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_hitbtc_health__only_use_cache_unless_empty()
        {
            var result = _client.GetHitBtcHealth(CachePolicyContract.OnlyUseCacheUnlessEmpty);
            result.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_open_orders__coss__force_refresh()
        {
            var result = _client.GetOpenOrders(IntegrationNameRes.Coss, CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_open_orders__coss__only_use_cache()
        {
            var result = _client.GetOpenOrders(IntegrationNameRes.Coss, CachePolicy.OnlyUseCache);
            result.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_open_orders__coss__allow_cache()
        {
            var result = _client.GetOpenOrders(IntegrationNameRes.Coss, CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_open_orders__coss__eth_btc__force_refresh()
        {
            var result = _client.GetOpenOrders(IntegrationNameRes.Coss, "ETH", "BTC", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_open_orders_v2__coss()
        {
            var result = _client.GetOpenOrdersV2(IntegrationNameRes.Coss);
            result.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_open_orders_v2__qryptos()
        {
            var result = _client.GetOpenOrdersV2(IntegrationNameRes.Qryptos);
            result.Where(item => item.OpenOrders != null && item.OpenOrders.Any())
                .ToList().Dump();
            // result.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_open_orders_for_trading_pair_v2__coss__link_eth__force_refresh()
        {
            var result = _client.GetOpenOrdersForTradingPairV2(IntegrationNameRes.Coss, "LINK", "ETH", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_open_orders_for_trading_pair_v2__coss__req_btc__force_refresh()
        {
            var result = _client.GetOpenOrdersForTradingPairV2(IntegrationNameRes.Coss, "REQ", "BTC", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_open_orders_for_trading_pair__kucoin__cs_eth__force_refresh()
        {
            var results = _client.GetOpenOrdersForTradingPairV2(IntegrationNameRes.KuCoin, "CS", "ETH", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Exchange_client__ping()
        {
            var result = _client.Ping();
            result.Dump();
        }

        [TestMethod]
        public void Exchange_client__get_commodities_for_exchange__coss()
        {
            var result = _client.GetCommoditiesForExchange(IntegrationNameRes.Coss, CachePolicy.AllowCache);
            result.Dump();
        }
    }
}
