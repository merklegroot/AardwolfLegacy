using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using test_shared;
using web_util;
using trade_model;
using coss_lib;
using Shouldly;
using trade_res;
using System.Linq;
using System.Collections.Generic;
using Moq;
using log_lib;
using dump_lib;
using res_util_lib;
using System.IO;
using cache_lib.Models;
using coss_cookie_lib;
using coss_browser_service_client;
using System.Threading;
using config_client_lib;
using exchange_client_lib;
using tfa_lib;
using trade_lib.Repo;
using cache_lib;
using coss_api_client_lib;
using trade_constants;
using coss_lib.Res;
using Newtonsoft.Json;

namespace coss_lib_integration_tests
{
    [TestClass]
    public class CossIntegrationTests
    {
        private ExchangeClient _exchangeClient;
        private CossIntegration _coss;

        [TestInitialize]
        public void Setup()
        {
            _exchangeClient = new ExchangeClient();
            var webUtil = new WebUtil();

            var configClient = new ConfigClient();
            var cossBrowserClient = new CossBrowserClient();
            var tfaUtil = new TfaUtil(webUtil);

            var log = new Mock<ILogRepo>();
            var cossCookieUtil = new CossCookieUtil(cossBrowserClient, log.Object);            

            var cossApiClient = new CossApiClient();
            var cacheUtil = new CacheUtil();

            var openOrdersSnapshotRepo = new OpenOrdersSnapshotRepo();

            _coss = new CossIntegration(
                webUtil,
                cossApiClient,
                configClient,
                cacheUtil,
                cossCookieUtil,
                configClient,
                tfaUtil,
                openOrdersSnapshotRepo,
                log.Object);
        }

        [TestMethod]
        public void Coss__get_old_withdrawal_fees()
        {
            var fees = CossRes.WithdrawalFees;
            Console.WriteLine(JsonConvert.SerializeObject(fees));
        }

        [TestMethod]
        public void Coss__get_commodities__alow_cache()
        {
            var coins = _coss.GetCommodities(CachePolicy.AllowCache);
            var canonicals = CommodityRes.All;
            var matches = new List<dynamic>();
            foreach (var coin in coins)
            {
                var candidates = 
                canonicals.Where(queryItem =>
                    string.Equals(queryItem.Symbol, coin.Symbol, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();

                matches.Add(new { coin, candidates });
            }

            coins.Dump();
        }

        [TestMethod]
        public void Coss__get_commodities__only_use_cache_unless_empty()
        {
            var results = _coss.GetCommodities(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Coss__get_commodities__force_refresh()
        {
            var results = _coss.GetCommodities(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Coss__get_user_trade_history__only_use_cache_unless_empty()
        {
            var result = _coss.GetUserTradeHistory(CachePolicy.OnlyUseCacheUnlessEmpty);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_user_trade_history_v2___only_use_cache_unless_empty()
        {
            var result = _coss.GetUserTradeHistoryV2(CachePolicy.OnlyUseCacheUnlessEmpty);
            result.Dump();
        }
        
        [TestMethod]
        public void Coss__get_native_completed_orders__coss_eth__allow_cache()
        {
            var result = _coss.GetNativeCompletedOrders("COSS", "ETH", CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_native_completed_orders__coss_eth__force_refresh()
        {
            var result = _coss.GetNativeCompletedOrders("COSS", "ETH", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_native_completed_orders__coss_btc__force_refresh()
        {
            var result = _coss.GetNativeCompletedOrders("COSS", "BTC", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_user_trade_history__coss_eth__allow_cache()
        {
            var result = _coss.GetUserTradeHistoryForTradingPair("COSS", "ETH", CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_user_trade_history__coss_eth__force_refresh()
        {
            var result = _coss.GetUserTradeHistoryForTradingPair("COSS", "ETH", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_user_trade_history_v2___allow_cache()
        {
            var result = _coss.GetUserTradeHistoryV2(CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Coss__refresh_history__allow_cache()
        {
            var tradingPairs = _coss.GetTradingPairs(CachePolicy.AllowCache);
            foreach(var tradingPair in tradingPairs)
            {
                _coss.GetUserTradeHistoryForTradingPair(tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.AllowCache);
                Thread.Sleep(TimeSpan.FromSeconds(2.5));
            }
        }

        [TestMethod]
        public void Coss__get_user_trade_history__npxs_btc__force_refresh()
        {
            var results = _coss.GetUserTradeHistoryForTradingPair("NPXS", "BTC", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Coss__get_user_trade_history__force_refresh()
        {
            var result = _coss.GetUserTradeHistory(CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_zen_withdrawal_history()
        {
            var result = _coss.GetUserTradeHistory(CachePolicy.AllowCache);
            result.Where(item => 
                string.Equals(item.Symbol, "ZEN", StringComparison.InvariantCultureIgnoreCase)
                && item.TradeType != TradeTypeEnum.Buy
                && item.TradeType != TradeTypeEnum.Sell)
                .OrderByDescending(item => item.TimeStampUtc)
                .ToList()
                .Dump();
        }

        [TestMethod]
        public void Coss__get_order_book__sub_eth__force_refresh()
        {
            var result = _coss.GetOrderBook(new TradingPair("SUB", "ETH"), CachePolicy.ForceRefresh);            
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_order_book__waves_eth__force_refresh()
        {
            var result = _coss.GetOrderBook(new TradingPair("WAVES", "ETH"), CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_order_book__coss_bchabc__force_refresh()
        {
            var result = _coss.GetOrderBook(new TradingPair("COSS", "BCHABC"), CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_order_book__coss_eth__force_refresh()
        {
            var result = _coss.GetOrderBook(new TradingPair("COSS", "ETH"), CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_order_book__snm_eth_repeatedly_force_refresh()
        {
            for (var i = 0; i < 10; i++)
            {
                _coss.GetOrderBook(new TradingPair("SNM", "ETH"), CachePolicy.ForceRefresh);
            }
        }

        [TestMethod]
        public void Coss__get_order_book__neo_eth__only_use_cache_unless_empty()
        {
            var result = _coss.GetOrderBook(new TradingPair("NEO", "ETH"), CachePolicy.OnlyUseCacheUnlessEmpty);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_order_book__snm_btc__force_refresh()
        {
            var result = _coss.GetOrderBook(new TradingPair("SNM", "BTC"), CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_order_book__ark_eth__only_use_cache()
        {
            var result = _coss.GetOrderBook(new TradingPair("ARK", "ETH"), CachePolicy.OnlyUseCache);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_order_book__lala_eth__only_use_cache_unless_empty()
        {
            var result = _coss.GetOrderBook(new TradingPair("LALA", "ETH"), CachePolicy.OnlyUseCacheUnlessEmpty);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_order_book__lsk_btc__only_use_cache_unless_empty()
        {
            var result = _coss.GetOrderBook(new TradingPair("LSK", "BTC"), CachePolicy.OnlyUseCacheUnlessEmpty);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_order_book__lsk_btc__allow_cache()
        {
            var result = _coss.GetOrderBook(new TradingPair("LSK", "BTC"), CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_order_book__lsk_btc__force_refresh()
        {
            var result = _coss.GetOrderBook(new TradingPair("LSK", "BTC"), CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_order_book__snm_btc__allow_cache()
        {
            var result = _coss.GetOrderBook(new TradingPair("SNM", "BTC"), CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_order_book__wish_eth__allow_cache()
        {
            var result = _coss.GetOrderBook(new TradingPair("WISH", "ETH"), CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_trading_pairs__force_refersh()
        {
            var results = _coss.GetTradingPairs(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Coss__get_trading_pairs__only_use_cache_unless_empty__coss_tusd()
        {
            var results = _coss.GetTradingPairs(CachePolicy.OnlyUseCacheUnlessEmpty);

            var match = results.SingleOrDefault(item => string.Equals(item.Symbol, "COSS", StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.BaseSymbol, "TUSD", StringComparison.InvariantCultureIgnoreCase));

            match.Dump();
        }

        [TestMethod]
        public void Coss__get_trading_pairs__only_use_cache_unless_empty__gat_eth()
        {
            var results = _coss.GetTradingPairs(CachePolicy.OnlyUseCacheUnlessEmpty);

            var match = results.SingleOrDefault(item => string.Equals(item.Symbol, "GAT", StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.BaseSymbol, "ETH", StringComparison.InvariantCultureIgnoreCase));

            match.Dump();
        }

        [TestMethod]
        public void Coss__get_trading_pairs__only_use_cache_unless_empty__gat_btc()
        {
            var results = _coss.GetTradingPairs(CachePolicy.OnlyUseCacheUnlessEmpty);

            var match = results.SingleOrDefault(item => string.Equals(item.Symbol, "GAT", StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.BaseSymbol, "BTC", StringComparison.InvariantCultureIgnoreCase));

            match.Dump();
        }

        [TestMethod]
        public void Coss__get_trading_pairs__allow_cache()
        {
            var results = _coss.GetTradingPairs(CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Coss__get_trading_pairs__coss_tusd__allow_cache()
        {
            var match = _coss.GetTradingPairs(CachePolicy.AllowCache)
                .SingleOrDefault(item => string.Equals(item.Symbol, "COSS", StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(item.BaseSymbol, "TUSD", StringComparison.InvariantCultureIgnoreCase));

            match.Dump();
        }

        [TestMethod]
        public void Coss__get_trading_pairs__only_use_cache()
        {
            var results = _coss.GetTradingPairs(CachePolicy.OnlyUseCache);
            results.Dump();
        }

        [TestMethod]
        public void Coss__get_deposit_addresses__force_refresh()
        {
            _coss.GetDepositAddresses(CachePolicy.ForceRefresh).Dump();
        }

        [TestMethod]
        public void Coss__get_deposit_address__USDC__force_refresh()
        {
            var result = _coss.GetDepositAddress("USDC", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_deposit_addresses__only_use_cache_unless_empty()
        {
            _coss.GetDepositAddresses(CachePolicy.OnlyUseCacheUnlessEmpty).Dump();
        }

        [TestMethod]
        public void Coss__get_withdrawal_fees()
        {
            _coss.GetWithdrawalFees(CachePolicy.ForceRefresh).Dump();
        }

        [TestMethod]
        public void Coss__get_req_withdrawal_fee()
        {
            var fee = _coss.GetWithdrawalFee("REQ", CachePolicy.ForceRefresh);
            Console.WriteLine($"Fee: {fee}");
            fee.ShouldBe(8);
        }

        [TestMethod]
        public void Coss__refresh_order_book()
        {
            var result = _coss.RefreshOrderBook(new TradingPair("ENJ", "ETH"));
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_holdings__allow_cache()
        {
            var result = _coss.GetHoldings(CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Coss__get_holdings__force_refresh()
        {
            var result = _coss.GetHoldings(CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Coss__cancel_open_orders__all__force_refresh()
        {
            Console.WriteLine($"Starting process. {DateTime.Now.ToString()} local time.");

            var tradingPairs = _coss.GetTradingPairs(CachePolicy.ForceRefresh);
            for (var i = 0; i < tradingPairs.Count; i++)
            {
                var tradingPair = tradingPairs[i];
                Console.WriteLine($"Pair {i + 1} of {tradingPairs.Count} - {tradingPair.Symbol}-{tradingPair.BaseSymbol}.");
                try
                {
                    var openOrders = _coss.GetOpenOrders(tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh) ?? new List<OpenOrderForTradingPair>();
                    if (!openOrders.Any()) { Console.WriteLine($"  There are no open orders or {tradingPair.Symbol}-{tradingPair.BaseSymbol}."); }
                    else { Console.WriteLine($"  There are {openOrders.Count} open orders for {tradingPair.Symbol}-{tradingPair.BaseSymbol}."); }

                    foreach (var openOrder in openOrders ?? new List<OpenOrderForTradingPair>())
                    {
                        try
                        {
                            Console.WriteLine($"  Cancelling order \"{openOrder.OrderId}\"");
                            _coss.CancelOrder(openOrder.OrderId);
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
        }

        [TestMethod]
        public void Coss__cancel_eth_btc_open_orders__force_refresh()
        {
            Console.WriteLine($"Starting process. {DateTime.Now.ToString()} local time.");

            var openOrders = _coss.GetOpenOrdersForTradingPairV2("ETH", "BTC", CachePolicy.ForceRefresh);
            if (openOrders?.OpenOrders == null || !openOrders.OpenOrders.Any())
            {
                Assert.Inconclusive("There are open orders matching that criteria.");
            }

            foreach (var openOrder in openOrders.OpenOrders)
            {
                _coss.CancelOrder(openOrder.OrderId);
            }

            var openOrdersAfterCancelling = _coss.GetOpenOrdersForTradingPairV2("ETH", "BTC", CachePolicy.ForceRefresh);
            (openOrders?.OpenOrders == null || !openOrders.OpenOrders.Any()).ShouldBe(true);

            Console.WriteLine($"Process completed. {DateTime.Now.ToString()} local time.");
        }

        [TestMethod]
        public void Coss__cancel_eth_open_orders__force_refresh()
        {
            Console.WriteLine($"Starting process. {DateTime.Now.ToString()} local time.");

            CancelAllOpenOrdersWithSymbolOrBaseSymbol("ETH");

            Console.WriteLine($"Process completed. {DateTime.Now.ToString()} local time.");
        }

        [TestMethod]
        public void Coss__cancel_open_orders_where_base_symbol_is_coss__force_refresh()
        {
            Console.WriteLine($"Starting process. {DateTime.Now.ToString()} local time.");

            CancelAllOpenOrdersWithBaseSymbol("COSS");

            Console.WriteLine($"Process completed. {DateTime.Now.ToString()} local time.");
        }

        [TestMethod]
        public void Coss__cancel_all_coss_open_orders()
        {
            Console.WriteLine($"Starting process. {DateTime.Now.ToString()} local time.");

            CancelAllOpenOrdersWithSymbolOrBaseSymbol("COSS");

            Console.WriteLine($"Process completed. {DateTime.Now.ToString()} local time.");
        }

        [TestMethod]
        public void Coss__cancel_all_tusd_open_orders()
        {
            Console.WriteLine($"Starting process. {DateTime.Now.ToString()} local time.");

            CancelAllOpenOrdersWithSymbolOrBaseSymbol("TUSD");

            Console.WriteLine($"Process completed. {DateTime.Now.ToString()} local time.");
        }

        [TestMethod]
        public void Coss__cancel_all_usdt_open_orders()
        {
            Console.WriteLine($"Starting process. {DateTime.Now.ToString()} local time.");

            CancelAllOpenOrdersWithSymbolOrBaseSymbol("USDT");

            Console.WriteLine($"Process completed. {DateTime.Now.ToString()} local time.");
        }

        [TestMethod]
        public void Coss__cancel_all_bchabc_open_orders()
        {
            Console.WriteLine($"Starting process. {DateTime.Now.ToString()} local time.");

            CancelAllOpenOrdersWithSymbolOrBaseSymbol("BCHABC");

            Console.WriteLine($"Process completed. {DateTime.Now.ToString()} local time.");
        }

        [TestMethod]
        public void Coss__cancel_all_ltc_open_orders()
        {
            Console.WriteLine($"Starting process. {DateTime.Now.ToString()} local time.");

            CancelAllOpenOrdersWithSymbolOrBaseSymbol("LTC");

            Console.WriteLine($"Process completed. {DateTime.Now.ToString()} local time.");
        }

        [TestMethod]
        public void Coss__cancel_all_la_open_orders()
        {
            Console.WriteLine($"Starting process. {DateTime.Now.ToString()} local time.");

            CancelAllOpenOrdersWithSymbolOrBaseSymbol("LA");

            Console.WriteLine($"Process completed. {DateTime.Now.ToString()} local time.");
        }


        [TestMethod]
        public void Coss__cancel_all_ark_open_orders()
        {
            Console.WriteLine($"Starting process. {DateTime.Now.ToString()} local time.");

            CancelAllOpenOrdersWithSymbolOrBaseSymbol("ARK");

            Console.WriteLine($"Process completed. {DateTime.Now.ToString()} local time.");
        }

        [TestMethod]
        public void Coss__cancel_all_bids_where_btc_is_the_base_symbol()
        {
            Console.WriteLine($"Starting process. {DateTime.Now.ToString()} local time.");

            CancelAllBidsUsingBaseSymbol("BTC");

            Console.WriteLine($"Process completed. {DateTime.Now.ToString()} local time.");
        }


        [TestMethod]
        public void Coss__cancel_all_dash_open_orders()
        {
            Console.WriteLine($"Starting process. {DateTime.Now.ToString()} local time.");

            CancelAllOpenOrdersWithSymbolOrBaseSymbol("DASH");

            Console.WriteLine($"Process completed. {DateTime.Now.ToString()} local time.");
        }

        [TestMethod]
        public void Coss__cancel_all_neo_open_orders()
        {
            Console.WriteLine($"Starting process. {DateTime.Now.ToString()} local time.");

            CancelAllOpenOrdersWithSymbolOrBaseSymbol("NEO");

            Console.WriteLine($"Process completed. {DateTime.Now.ToString()} local time.");
        }

        [TestMethod]
        public void Coss__cancel_all_zen_open_orders()
        {
            Console.WriteLine($"Starting process. {DateTime.Now.ToString()} local time.");

            CancelAllOpenOrdersWithSymbolOrBaseSymbol("ZEN");

            Console.WriteLine($"Process completed. {DateTime.Now.ToString()} local time.");
        }

        private void CancelAllOpenOrdersWithBaseSymbol(string baseSymbol)
        {
            var tradingPairs = _coss.GetTradingPairs(CachePolicy.ForceRefresh).Where(item => string.Equals(item.BaseSymbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase)).ToList();
            for (var i = 0; i < tradingPairs.Count; i++)
            {
                var tradingPair = tradingPairs[i];
                Console.WriteLine($"Pair {i + 1} of {tradingPairs.Count} - {tradingPair.Symbol}-{tradingPair.BaseSymbol}.");
                try
                {
                    var openOrders = _coss.GetOpenOrders(tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh) ?? new List<OpenOrderForTradingPair>();
                    if (!openOrders.Any()) { Console.WriteLine($"  There are no open orders or {tradingPair.Symbol}-{tradingPair.BaseSymbol}."); }
                    else { Console.WriteLine($"  There are {openOrders.Count} open orders for {tradingPair.Symbol}-{tradingPair.BaseSymbol}."); }

                    foreach (var openOrder in openOrders ?? new List<OpenOrderForTradingPair>())
                    {
                        try
                        {
                            Console.WriteLine($"  Cancelling order \"{openOrder.OrderId}\"");
                            _coss.CancelOrder(openOrder.OrderId);
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
        }

        private void CancelAllBidsUsingBaseSymbol(string baseSymbol)
        {
            var tradingPairs = _coss.GetTradingPairs(CachePolicy.ForceRefresh)
                .Where(item =>
                string.Equals(item.BaseSymbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase)
                ).ToList();
            for (var i = 0; i < tradingPairs.Count; i++)
            {
                var tradingPair = tradingPairs[i];
                Console.WriteLine($"Pair {i + 1} of {tradingPairs.Count} - {tradingPair.Symbol}-{tradingPair.BaseSymbol}.");
                try
                {
                    var bids = (_coss.GetOpenOrders(tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh) ?? new List<OpenOrderForTradingPair>())
                            .Where(item => item.OrderType == OrderType.Bid).ToList();

                    if (!bids.Any()) { Console.WriteLine($"  There are no open orders or {tradingPair.Symbol}-{tradingPair.BaseSymbol}."); }
                    else { Console.WriteLine($"  There are {bids.Count} open orders for {tradingPair.Symbol}-{tradingPair.BaseSymbol}."); }

                    foreach (var openOrder in bids ?? new List<OpenOrderForTradingPair>())
                    {
                        try
                        {
                            Console.WriteLine($"  Cancelling order \"{openOrder.OrderId}\"");
                            _coss.CancelOrder(openOrder.OrderId);
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
        }

        private void CancelAllOpenOrdersWithSymbolOrBaseSymbol(string symbolOrBaseSymbol)
        {
            var tradingPairs = _coss.GetTradingPairs(CachePolicy.ForceRefresh)
                .Where(item => 
                string.Equals(item.BaseSymbol, symbolOrBaseSymbol, StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(item.Symbol, symbolOrBaseSymbol, StringComparison.InvariantCultureIgnoreCase)
                ).ToList();

            for (var i = 0; i < tradingPairs.Count; i++)
            {
                var tradingPair = tradingPairs[i];
                Console.WriteLine($"Pair {i + 1} of {tradingPairs.Count} - {tradingPair.Symbol}-{tradingPair.BaseSymbol}.");
                try
                {
                    var openOrders = _coss.GetOpenOrders(tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh) ?? new List<OpenOrderForTradingPair>();
                    if (!openOrders.Any()) { Console.WriteLine($"  There are no open orders or {tradingPair.Symbol}-{tradingPair.BaseSymbol}."); }
                    else { Console.WriteLine($"  There are {openOrders.Count} open orders for {tradingPair.Symbol}-{tradingPair.BaseSymbol}."); }

                    foreach (var openOrder in openOrders ?? new List<OpenOrderForTradingPair>())
                    {
                        try
                        {
                            Console.WriteLine($"  Cancelling order \"{openOrder.OrderId}\"");
                            _coss.CancelOrder(openOrder.OrderId);
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
        }


        [TestMethod]
        public void Coss__generate_commodity_map()
        {
            var commodities = _coss.GetCommodities(CachePolicy.AllowCache);
            commodities
                .Where(item => item.CanonicalId.HasValue)
                .Select(item => new
                {
                    CanonicalId = item.CanonicalId.Value,
                    NativeSymbol = item.NativeSymbol,
                }).ToList()
                .Dump();
        }

        [TestMethod]
        public void Add_canonical_commodity()
        {
            var allCanon = CommodityRes.All;
            var newCanon = new Commodity
            {
                Id = Guid.NewGuid(),
                Symbol = "VEN",
                Name = "VeChain",
                IsEth = false,
                IsEthToken = true,
                ContractId = "0xd850942ef8811f2a866692a623011bde52a462c1",
                Decimals = 18
            };

            allCanon.Add(newCanon);

            var json = allCanon.ToJson();
            const string FileName = @"C:\repo\trade-ex\trade-res\Resources\canon.json";

            File.WriteAllText(FileName, json);
        }

        [TestMethod]
        public void Coss__coerce_additional_mapped_items()
        {
            var cossMap = ResUtil.Get<List<CommodityMapItem>>("coss-map.json", _coss.GetType().Assembly);
            var autoMapSymbols = new List<string>
            {
                "MRK"
            };

            var commodities = _coss.GetCommodities(CachePolicy.AllowCache);

            var commoditiesWithoutMatches = commodities.Where(item => !item.CanonicalId.HasValue)
                .ToList();

            // commoditiesWithoutMatches.Select(item => $"{item.NativeName} ({item.NativeSymbol})").ToList().Dump();

            var matchCount = 0;
            foreach (var commodity in commoditiesWithoutMatches.ToList())
            {
                var match = GetCanonicalMatch(commodity);
                if(match == null) { continue; }
                match.Dump();
                matchCount++;
                if (matchCount >= 10) { break; }
            }
            
            foreach (var autoMapSymbol in autoMapSymbols)
            {
                var commodity = commodities.Single(com => string.Equals(com.NativeSymbol, autoMapSymbol, StringComparison.InvariantCultureIgnoreCase));
                var canon = GetCanonicalMatch(commodity);
                if (canon == null) { continue; }
                if (cossMap.Any(existingMap => existingMap.CanonicalId == canon.Id)) { continue; }

                cossMap.Add(new CommodityMapItem
                {
                    CanonicalId = canon.Id,
                    NativeSymbol = commodity.NativeSymbol
                });
            }

            var json = cossMap.ToJson();
            const string FileName = @"C:\repo\trade-ex\coss-lib\Res\coss-map.json";

            File.WriteAllText(FileName, json);            
        }

        [TestMethod]
        public void Coss__get_cached_order_books()
        {
            var results = _coss.GetCachedOrderBooks();
            results.Dump();
        }

        [TestMethod]
        public void Coss__get_cached_order_books__neo_eth()
        {
            var results = _coss.GetCachedOrderBooks();
            var neoEth = results.SingleOrDefault(item =>
                string.Equals(item.Symbol, "NEO", StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.BaseSymbol, "ETH", StringComparison.InvariantCultureIgnoreCase));

            neoEth.Dump();
        }


        [TestMethod]
        public void Coss__get_open_orders__force_refresh()
        {
            var results = _coss.GetOpenOrders(CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Coss__get_open_orders__allow_cache()
        {
            var results = _coss.GetOpenOrders(CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Coss__get_open_orders__only_use_cache_unless_empty()
        {
            var results = _coss.GetOpenOrders(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Coss__cancel_all_known_open_orders()
        {
            var allKnownOpenOrders = _coss.GetOpenOrders(CachePolicy.OnlyUseCacheUnlessEmpty);
            var tradingPairs = allKnownOpenOrders.Select(item => $"{item.Symbol.ToUpper()}_{item.BaseSymbol.ToUpper()}")
                    .Distinct()
                    .Select(item =>
                    {
                        var pieces = item.Split('_');
                        return new TradingPair { Symbol = pieces[0], BaseSymbol = pieces[1] };
                    }).ToList();

            for (var i = 0; i < tradingPairs.Count; i++)
            {
                try
                {
                    var tradingPair = tradingPairs[i];
                    var openOrdersForTradingPair = _coss.GetOpenOrders(tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh);
                    for (var j = 0; j < openOrdersForTradingPair.Count; j++)
                    {
                        var openOrder = openOrdersForTradingPair[j];
                        _coss.CancelOrder(openOrder.OrderId);
                    }

                    if (openOrdersForTradingPair.Any())
                    {
                        var refreshedOpenOrders = _coss.GetOpenOrders(tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh);
                    }
                } catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    Thread.Sleep(TimeSpan.FromSeconds(15));
                }
            }
        }

        [TestMethod]
        public void Coss__get_open_orders__dash_btc__only_use_cache_unless_empty__v2()
        {
            var results = _coss.GetOpenOrdersForTradingPairV2("DASH", "BTC", CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
        }

        [TestMethod]
        public void Coss__get_open_orders__dash_btc__force_refresh()
        {
            var results = _coss.GetOpenOrders("DASH", "BTC", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Coss__get_open_orders__dash_coss__force_refresh()
        {
            var results = _coss.GetOpenOrders("DASH", "COSS", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Coss__get_open_orders__ark_eth__force_refresh()
        {
            var results = _coss.GetOpenOrders("ARK", "ETH", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Coss__get_open_orders__tusd__force_refresh()
        {
            var tradingPairs = _coss.GetTradingPairs(CachePolicy.ForceRefresh);
            var tusdTradingPairs = tradingPairs.Where(item => string.Equals(item.BaseSymbol, "TUSD", StringComparison.InvariantCultureIgnoreCase));

            foreach (var tradingPair in tusdTradingPairs)
            {
                var openOrders = _coss.GetOpenOrders(tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh);
                openOrders.Dump();
            }
        }

        [TestMethod]
        public void Coss__get_open_orders__eth_btc__force_refresh()
        {
            var results = _coss.GetOpenOrders("ETH", "BTC", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Coss__get_open_orders__eth_btc__allow_cache()
        {
            var results = _coss.GetOpenOrders("ETH", "BTC", CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Coss__get_open_orders__fyn_btc__force_refresh()
        {
            var results = _coss.GetOpenOrders("FYN", "BTC", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Coss__get_open_orders__coss_bch__force_refresh()
        {
            var results = _coss.GetOpenOrders("COSS", "BCH", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Coss__get_open_orders__omg_coss__force_refresh()
        {
            var results = _coss.GetOpenOrders("OMG", "COSS", CachePolicy.ForceRefresh);
            results.Dump();
        }

        [TestMethod]
        public void Coss__get_open_orders__v2()
        {
            var results = _coss.GetOpenOrdersV2();
            results.Dump();
        }

        [TestMethod]
        public void Coss__cancel_order()
        {
            const string OrderId = "{\"Id\":\"1f470214-17e6-4df6-9ce4-0c43e110af85\",\"NativeSymbol\":\"ETH\",\"NativeBaseSymbol\":\"BTC\"}";
            _coss.CancelOrder(OrderId);
        }

        [TestMethod]
        public void Coss__cancel_open_orders__omg_coss()
        {
            const string Symbol = "OMG";
            const string BaseSymbol = "COSS";
            var openOrders = _coss.GetOpenOrders(Symbol, BaseSymbol, CachePolicy.ForceRefresh);
            if (openOrders == null || !openOrders.Any())
            {
                Assert.Inconclusive($"There are no open orders to cancel for {Symbol}-{BaseSymbol}.");
            }

            foreach(var openOrder in openOrders)
            {
                _coss.CancelOrder(openOrder.OrderId);
            }

            var openOrdersAfterCancelling = _coss.GetOpenOrders(Symbol, BaseSymbol, CachePolicy.ForceRefresh);
            (openOrdersAfterCancelling ?? new List<OpenOrderForTradingPair>()).Count.ShouldBe(0);
        }

        [TestMethod]
        public void Coss__sell_limit__pix_eth()
        {
            bool shouldActuallySell = false;
            if (!shouldActuallySell)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            var symbol = "PIX";
            var baseSymbol = "ETH";
            var quantity = 500.0050000000000m;
            var price = 0.0000668393316066839331606684m;

            _coss.SellLimit(symbol, baseSymbol, new QuantityAndPrice { Quantity = quantity, Price = price });
        }

        [TestMethod]
        public void Coss__sell_limit__pay_eth()
        {
            bool shouldActuallySell = false;
            if (!shouldActuallySell)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string symbol = "PAY";
            const string baseSymbol = "ETH";
            const decimal quantity = 138.99m;
            const decimal price = 0.004566000000m;

            _coss.SellLimit(symbol, baseSymbol, new QuantityAndPrice { Quantity = quantity, Price = price });
        }

        [TestMethod]
        public void Coss__sell_limit__coss_bch()
        {
            bool shouldActuallySell = false;
            if (!shouldActuallySell)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string symbol = "COSS";
            const string baseSymbol = "BCH";
            const decimal quantity = 500.0m;
            const decimal price = 0.00023995m;

            _coss.SellLimit(symbol, baseSymbol, new QuantityAndPrice { Quantity = quantity, Price = price });
        }

        [TestMethod]
        public void Coss__buy_limit__req_eth()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            var symbol = "REQ";
            var baseSymbol = "ETH";
            var quantity = 0.3600144005760230409216368655m * 10.0m;
            var price = 0.0001666616666m;

            var result = _coss.BuyLimit(symbol, baseSymbol, new QuantityAndPrice { Quantity = quantity, Price = price });
            result.Dump();

            result.WasSuccessful.ShouldBe(true);
        }

        [TestMethod]
        public void Coss__buy_limit__lala_btc()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string Symbol = "LALA";
            const string BaseSymbol = "BTC";
            const decimal Quantity = 70415;
            // const decimal Quantity = 70414;
            // const decimal Price = 0.000000710070m;
            const decimal Price = 0.00000071m;

            var result = _coss.BuyLimit(Symbol, BaseSymbol, new QuantityAndPrice { Quantity = Quantity, Price = Price });
            result.Dump();

            result.WasSuccessful.ShouldBe(true);
        }

        [TestMethod]
        public void Coss__buy_limit__wish_eth__bad_lot_size()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            Exception exception = null;
            try
            {
                var symbol = "WISH";
                var baseSymbol = "ETH";
                var quantity = 12.0m;
                var price = 0.00025m;

                var result = _coss.BuyLimit(symbol, baseSymbol, new QuantityAndPrice { Quantity = quantity, Price = price });
                result.Dump();

                result.WasSuccessful.ShouldBe(false);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            exception.ShouldNotBeNull();
            exception.Dump();
        }

        [TestMethod]
        public void Coss__buy_limit__eth_btc()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string symbol = "ETH";
            const string baseSymbol = "BTC";
            const decimal quantity = 0.03m;
            const decimal price = 0.02m;

            var result = _coss.BuyLimit(symbol, baseSymbol, new QuantityAndPrice { Quantity = quantity, Price = price });
            result.Dump();

            result.WasSuccessful.ShouldBe(true);
        }

        [TestMethod]
        public void Coss__sell_limit__eth_btc()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string symbol = "ETH";
            const string baseSymbol = "BTC";
            const decimal quantity = 0.01m;
            const decimal price = 0.035m;

            var result = _coss.SellLimit(symbol, baseSymbol, new QuantityAndPrice { Quantity = quantity, Price = price });
            result.Dump();

            result.WasSuccessful.ShouldBe(true);
        }


        [TestMethod]
        public void Coss__buy_limit__dash_tusd()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string symbol = "DASH";
            const string baseSymbol = "TUSD";
            const decimal quantity = 1.1817832100000000000000000000m;
            const decimal price = 150.00000001000000m;

            var result = _coss.BuyLimit(symbol, baseSymbol, new QuantityAndPrice { Quantity = quantity, Price = price });
            result.Dump();

            result.WasSuccessful.ShouldBe(true);
        }

        [TestMethod]
        public void Coss__sell_limit__dash_btc()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string symbol = "DASH";
            const string baseSymbol = "BTC";
            const decimal quantity = 0.00017178m;
            const decimal price = 0.02316952m;

            var result = _coss.SellLimit(symbol, baseSymbol, new QuantityAndPrice { Quantity = quantity, Price = price });
            result.Dump();

            result.WasSuccessful.ShouldBe(true);
        }

        [TestMethod]
        public void Coss__buy_limit__ark_eth()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string symbol = "ARK";
            const string baseSymbol = "ETH";
            const decimal quantity = 1.0m;
            const decimal price = 0.003347m;

            var result = _coss.BuyLimit(symbol, baseSymbol, new QuantityAndPrice { Quantity = quantity, Price = price });
            result.Dump();

            result.WasSuccessful.ShouldBe(true);
        }


        [TestMethod]
        public void Coss__buy_limit__neo_coss()
        {
            bool shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const string symbol = "NEO";
            const string baseSymbol = "COSS";
            const decimal quantity = 0.4080816448297094872008818144m;
            const decimal price = 245.024500020001m;

            var result = _coss.BuyLimit(symbol, baseSymbol, new QuantityAndPrice { Quantity = quantity, Price = price });
            result.Dump();

            result.WasSuccessful.ShouldBe(true);
        }

        [TestMethod]
        public void Coss__get_session()
        {
            var session = _coss.GetSession();
            session.Dump();
        }

        [TestMethod]
        public void Coss__get_native_user_deposits_and_withdrawals_history__only_use_cache_unless_empty()
        {
            var response = _coss.GetNativeUserDepositsAndWithdrawalsHistory(CachePolicy.OnlyUseCacheUnlessEmpty);
            response.Dump();
        }

        [TestMethod]
        public void Coss__get_native_user_deposits_and_withdrawals_history__ark__only_use_cache_unless_empty()
        {
            var response = _coss.GetNativeUserDepositsAndWithdrawalsHistory(CachePolicy.OnlyUseCacheUnlessEmpty);
            var arkResults = response.payload.items.Where(item => string.Equals(item.currency_code, "ARK", StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            arkResults.Dump();
        }

        [TestMethod]
        public void Coss__withdraw_pix_to_hitbtc()
        {
            var shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            var commodity = CommodityRes.Lampix;
            const string DesintationExchange = IntegrationNameRes.HitBtc;

            var depositAddress = _exchangeClient.GetDepositAddress(DesintationExchange, commodity.Symbol, CachePolicy.AllowCache);
            if (depositAddress == null || string.IsNullOrWhiteSpace(depositAddress.Address))
            {
                throw new ApplicationException("Failed to get ETH deposit address from HitBTC.");
            }

            var balance = _coss.GetHolding(commodity.Symbol, CachePolicy.ForceRefresh);
            
            var amountToWithdraw = (int)balance.Available;

            const decimal Minimum = 1000;
            if (amountToWithdraw < Minimum)
            {
                throw new ApplicationException("You don't own enough for this to be worthwhile.");
            }
            
            var result = _coss.Withdraw(commodity, amountToWithdraw, depositAddress);
            result.Dump();
        }

        [TestMethod]
        public void Coss__withdraw_ark_to_binance()
        {
            var shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            var commodity = CommodityRes.Ark;
            const string DesintationExchange = IntegrationNameRes.Binance;

            var destinationCommodity = _exchangeClient.GetCommoditiyForExchange(DesintationExchange, commodity.Symbol, null, CachePolicy.AllowCache);

            var depositAddress = _exchangeClient.GetDepositAddress(DesintationExchange, commodity.Symbol, CachePolicy.AllowCache);
            if (depositAddress == null || string.IsNullOrWhiteSpace(depositAddress.Address))
            {
                throw new ApplicationException($"Failed to get {commodity.Name} deposit address from HitBTC.");
            }

            var balance = _coss.GetHolding(commodity.Symbol, CachePolicy.ForceRefresh);

            var amountToWithdraw = balance.Available;

            const decimal Minimum = 5;
            if (amountToWithdraw < Minimum)
            {
                throw new ApplicationException("You don't own enough for this to be worthwhile.");
            }

            var result = _coss.Withdraw(commodity, amountToWithdraw, depositAddress);
            result.Dump();
        }

        [TestMethod]
        public void Coss__withdraw_gat_to_kucoin()
        {
            var shouldRun = false;
            if (!shouldRun)
            {
                throw new ApplicationException("This test works with real funds and must be run manually.");
            }

            const decimal Quantity = 17461.42412280m;
            const decimal WithdrawalFee = 10;
            const decimal EffectiveQuantity = Quantity - WithdrawalFee;
            const string Symbol = "GAT";
            const string DestinationExchange = IntegrationNameRes.KuCoin;

            var destinationAddress = _exchangeClient.GetDepositAddress(DestinationExchange, Symbol, CachePolicy.AllowCache);
            
            var result = _coss.Withdraw(new Commodity { Symbol = Symbol }, EffectiveQuantity, destinationAddress);
            result.Dump();
        }

        private Commodity GetCanonicalMatch(CommodityForExchange commodity)
        {
            return CommodityRes.All.SingleOrDefault(queryCanon => 
                string.Equals(queryCanon.Symbol, commodity.NativeSymbol, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
