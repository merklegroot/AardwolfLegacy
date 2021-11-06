using System;
using System.Linq;
using System.Threading;
using bit_z_lib;
using bitz_data_lib;
using cache_lib.Models;
using dump_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using trade_model;
using trade_node_integration;
using web_util;
using config_client_lib;
using cache_lib;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace bit_z_lib_tests
{
    [TestClass]
    public class BitzRepoTests
    {
        private BitzIntegration _integration;

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            var log = new Mock<ILogRepo>();
            var configClient = new ConfigClient();
            var tradeNodeUtil = new TradeNodeUtil(configClient, webUtil, log.Object);            
            var fundRepo = new BitzFundsRepo(configClient);
            var bitzClient = new BitzClient();
            var cacheUtil = new CacheUtil();

            _integration = new BitzIntegration(bitzClient, configClient, tradeNodeUtil, webUtil, fundRepo, configClient, cacheUtil, log.Object);
        }

        [TestMethod]
        public void Bitz__get_order_book__ark_btc__force_refresh()
        {
            var result = _integration.GetOrderBook(new TradingPair("ARK", "BTC"), CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Bitz__get_order_book__eth_btc__force_refresh()
        {
            var result = _integration.GetOrderBook(new TradingPair("ETH", "BTC"), CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Bitz__get_cached_order_books()
        {
            var result = _integration.GetCachedOrderBooks();
            result.Dump();
        }

        [TestMethod]
        public void Bitz__get_trading_pairs__force_refresh()
        {
            var result = _integration.GetTradingPairs(CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Bitz__get_trading_pairs__allow_cache()
        {
            var result = _integration.GetTradingPairs(CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Bitz__get_trading_pairs__tusd_btc__only_use_cache_unless_empty()
        {
            var result = _integration.GetTradingPairs(CachePolicy.OnlyUseCacheUnlessEmpty);
            var match = result.Single(item => string.Equals(item.Symbol, "TUSD", StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(item.BaseSymbol, "BTC", StringComparison.InvariantCultureIgnoreCase));

            match.Dump();
        }

        [TestMethod]
        public void Bitz__get_trading_pairs__only_use_cache()
        {
            var result = _integration.GetTradingPairs(CachePolicy.OnlyUseCache);
            result.Dump();
        }

        [TestMethod]
        public void Bitz__get_holdings__force_refresh()
        {
            _integration.GetHoldings(CachePolicy.ForceRefresh).Dump();
        }

        [TestMethod]
        public void Bitz__get_holdings__only_use_cache_unless_empty()
        {
            var results = _integration.GetHoldings(CachePolicy.OnlyUseCacheUnlessEmpty);
            results.Dump();
            // .Dump();
        }

        [TestMethod]
        public void Bitz__get_commodities__force_refresh()
        {
            _integration.GetCommodities(CachePolicy.ForceRefresh)
                .Dump();
        }

        [TestMethod]
        public void Bitz__get_commodities__allow_cache()
        {
            _integration.GetCommodities(CachePolicy.AllowCache)
                .Dump();
        }

        [TestMethod]
        public void Bitz__get_ark_commodity()
        {
            _integration.GetCommodities(CachePolicy.ForceRefresh)
                .Single(item => string.Equals(item.Symbol, "ARK", StringComparison.InvariantCultureIgnoreCase))
                .Dump();
        }

        [TestMethod]
        public void Bitz__get_pix_commodity()
        {
            var commodities = _integration.GetCommodities(CachePolicy.ForceRefresh);
            var pix = commodities.Single(item => string.Equals(item.Symbol, "PIX", StringComparison.InvariantCultureIgnoreCase));
            pix.Dump();
        }

        [TestMethod]
        public void Bitz__buy_limit__ark_btc()
        {
            bool shouldRun = false;
            if (!shouldRun) { throw new ApplicationException("This test works with real funds and must be run manually."); }

            const decimal Quantity = 10.0m;
            const decimal Price = 0.00010755m;
            const string Symbol = "ARK";
            const string BaseSymbol = "BTC";

            var results = _integration.BuyLimit(new TradingPair(Symbol, BaseSymbol), Quantity, Price);
            results.Dump();
        }

        [TestMethod]
        public void Bitz__sell_limit()
        {
            bool shouldRun = false;
            if (!shouldRun)
            { throw new ApplicationException("This test sells real commodities and must be run manually."); }

            var quantity = 0.05m;
            var price = 0.0807m * 1.02m;
            var symbol = "DGB";
            var baseSymbol = "BTC";

            var result = _integration.SellLimit(new TradingPair(symbol, baseSymbol), quantity, price);
            result.Dump();
        }

        [TestMethod]
        public void Bitz__sell_dgb_limit()
        {
            bool shouldRun = false;
            if (!shouldRun)
            { throw new ApplicationException("This test sells real commodities and must be run manually."); }

            var quantity = 49.45m;
            var price = 0.00000648m;
            var symbol = "DGB";
            var baseSymbol = "BTC";

            var result = _integration.SellLimit(new TradingPair(symbol, baseSymbol), quantity, price);
            result.Dump();
        }

        [TestMethod]
        public void Bitz__get_native_markets__force_refresh()
        {
            _integration.GetNativeMarkets(CachePolicy.ForceRefresh)
                .Dump();
        }

        [TestMethod]
        public void Bitz__get_open_orders__xrb_btc__force_refresh()
        {
            _integration.GetOpenOrdersForTradingPair(new TradingPair("XRB", "BTC"), CachePolicy.ForceRefresh)
                .Dump();
        }

        [TestMethod]
        public void Bitz__get_open_orders__ark_btc__force_refresh()
        {
            _integration.GetOpenOrdersForTradingPair(new TradingPair("ARK", "BTC"), CachePolicy.ForceRefresh)
                .Dump();
        }

        [TestMethod]
        public void Bitz__get_open_orders__npxs_btc__force_refresh()
        {
            _integration.GetOpenOrdersForTradingPair(new TradingPair("NPXS", "BTC"), CachePolicy.ForceRefresh)
                .Dump();
        }

        [TestMethod]
        public void Bitz__get_open_orders_v2__tky_eth__force_refresh()
        {
            _integration.GetOpenOrdersForTradingPairV2("TKY", "ETH", CachePolicy.ForceRefresh)
                .Dump();
        }

        [TestMethod]
        public void Bitz__get_open_orders_with_client()
        {
            var results = _integration.GetClientOpenOrders(CachePolicy.AllowCache);
            results.Dump();
        }

        [TestMethod]
        public void Bitz__get_open_orders__force_refresh()
        {
            _integration.GetOpenOrders(CachePolicy.ForceRefresh)
                .Dump();
        }

        [TestMethod]
        public void Bitz__get_open_orders__allow_cache()
        {
            _integration.GetOpenOrders(CachePolicy.AllowCache)
                .Dump();
        }

        [TestMethod]
        public void Bitz__get_open_orders__only_use_cache()
        {
            _integration.GetOpenOrders(CachePolicy.OnlyUseCache)
                .Dump();
        }

        [TestMethod]
        public void Bitz__get_open_orders__only_use_cache_unless_empty()
        {
            _integration.GetOpenOrders(CachePolicy.OnlyUseCacheUnlessEmpty)
                .Dump();
        }

        [TestMethod]
        public void Bitz__cancel_all_open_npxs_btc_orders()
        {
            _integration.CancelAllOpenOrdersForTradingPair(new TradingPair("NPXS", "BTC"));
        }

        [TestMethod]
        public void Bitz__cancel_all_open_tky_eth_orders_individually()
        {
            const string Symbol = "TKY";
            const string BaseSymbol = "ETH";

            var openOrdersResponse = _integration.GetOpenOrdersForTradingPairV2(Symbol, BaseSymbol, CachePolicy.ForceRefresh);
            if (openOrdersResponse.OpenOrders == null || !openOrdersResponse.OpenOrders.Any())
            {
                Assert.Inconclusive($"There are no open orders for {Symbol}-{BaseSymbol} to cancel.");
            }

            foreach(var openOrder in openOrdersResponse.OpenOrders)
            {
                _integration.CancelOrder(openOrder.OrderId);
            }

            var openOrdersResponseAfterCancelling = _integration.GetOpenOrdersForTradingPairV2(Symbol, BaseSymbol, CachePolicy.ForceRefresh);

            (openOrdersResponseAfterCancelling.OpenOrders == null || !openOrdersResponseAfterCancelling.OpenOrders.Any())
                .ShouldBe(true);
        }

        [TestMethod]
        public void Bitz__cancel_all_open_ark_btc_orders_individually()
        {
            const string Symbol = "ARK";
            const string BaseSymbol = "BTC";

            var openOrdersResponse = _integration.GetOpenOrdersForTradingPairV2(Symbol, BaseSymbol, CachePolicy.ForceRefresh);
            if (openOrdersResponse.OpenOrders == null || !openOrdersResponse.OpenOrders.Any())
            {
                Assert.Inconclusive($"There are no open orders for {Symbol}-{BaseSymbol} to cancel.");
            }

            foreach (var openOrder in openOrdersResponse.OpenOrders)
            {
                _integration.CancelOrder(openOrder.OrderId);
            }

            var openOrdersResponseAfterCancelling = _integration.GetOpenOrdersForTradingPairV2(Symbol, BaseSymbol, CachePolicy.ForceRefresh);

            (openOrdersResponseAfterCancelling.OpenOrders == null || !openOrdersResponseAfterCancelling.OpenOrders.Any())
                .ShouldBe(true);
        }

        [TestMethod]
        public void Bitz__cancel_open_order_for_trading_pair()
        {
            bool shouldRun = false;
            if (!shouldRun) { return; }

            var tradingPair = new TradingPair("ARK", "BTC");

            const decimal Quantity = 1.5m;
            const decimal Price = 0.0002m;
            Console.WriteLine($"Creating a buy limit order for {Quantity} {tradingPair} at {Price}");
            _integration.BuyLimit(tradingPair, Quantity, Price);

            var timeToSleep = TimeSpan.FromSeconds(5);
            Console.WriteLine($"Sleeping for {timeToSleep.TotalSeconds} seconds so as not to overload their system.");
            Thread.Sleep(timeToSleep);

            var openOrders = _integration.GetOpenOrdersForTradingPair(tradingPair, CachePolicy.ForceRefresh);
            Console.WriteLine($"Open orders for {tradingPair}:");
            openOrders.Dump();
            openOrders.ShouldNotBeEmpty();

            Console.WriteLine($"Cancelling all open order for {tradingPair}.");
            _integration.CancelAllOpenOrdersForTradingPair(tradingPair);

            var openOrdersAfterCancelling = _integration.GetOpenOrdersForTradingPair(tradingPair, CachePolicy.ForceRefresh);
            Console.WriteLine("Open orders after cancelling:");
            openOrdersAfterCancelling.Dump();

            openOrdersAfterCancelling.ShouldBeEmpty();
        }

        [TestMethod]
        public void Bitz__get_eth_deposit_address()
        {
            var result = _integration.GetDepositAddress("ETH", CachePolicy.ForceRefresh);
            result.Dump();
        }

        [TestMethod]
        public void Bitz__get_deposit_addresses()
        {
            var result = _integration.GetDepositAddresses(CachePolicy.AllowCache);
            result.Dump();
        }

        [TestMethod]
        public void Bitz__get_history()
        {
            var results = _integration.GetUserTradeHistory(CachePolicy.AllowCache);
            results.Dump();
        }

        private class ResultsContainer
        {
            public DateTime StartTime { get; set;  }

            public DateTime EndTime { get; set; }

            public int Page { get; set; }

            public BitzHistoryResponse Results { get; set;  }
        }

        public class BitzHistoryResponse
        {
            public int Status { get; set; }
            public string Msg { get; set; }
            public BitzHistoryResponseData Data { get; set; }

            public class BitzHistoryResponseData
            {
                public List<BitzHistoryItem> Data { get; set; }

                public class BitzHistoryItem
                {
                    public long Id { get; set; }
                    public long Uid { get; set; }
                    public decimal Price { get; set; }
                    public decimal Number { get; set; }
                    public decimal Total { get; set; }
                    public decimal NumberOver { get; set; }
                    public decimal NumberDeal { get; set; }
                    public string Flag { get; set; }
                    public int Status { get; set; }
                    public string IsNew { get; set; }
                    public string CoinFrom { get; set; }
                    public string CoinTo { get; set; }
                    public int TradeType { get; set; }
                    public long Created { get; set; }
                }
            }
        }

        [TestMethod]
        public void Bitz__get_history_from_client()
        {
            var allResults = new List<ResultsContainer>();

            var range = 25;
            var startTime = new DateTime(2017, 12, 31);
            for (var i = 0; i < 15; i++)
            {
                var endTime = startTime.AddDays(range);
                for (var page = 1; page <= 20; page++)
                {
                    if (i != 0 || page != 1) { Thread.Sleep(TimeSpan.FromSeconds(5)); }

                    var responseText = _integration.GetHistoryFromClient(startTime, endTime, page);
                    var results = !string.IsNullOrWhiteSpace(responseText)
                        ? JsonConvert.DeserializeObject<BitzHistoryResponse>(responseText)
                        : null;

                    var container = new ResultsContainer
                    {
                        StartTime = startTime,
                        EndTime = endTime,
                        Page = page,
                        Results = results
                    };

                    var json = JsonConvert.SerializeObject(container, Formatting.Indented);
                    var namePiece = $"{startTime.ToString("yyyy-MM-dd")}__{page}.json";
                    var fileName = $"C:\\taxes-2018\\crypto\\bit-z\\{namePiece}";
                    File.WriteAllText(fileName, json);

                    allResults.Add(container);

                    if (results?.Data?.Data == null || results.Data.Data.Count() < 100)
                    {
                        break;
                    }
                }

                startTime = startTime.AddDays(range - 1);
            }

            allResults.Dump();
        }

        [TestMethod]
        public void Bitz__get_ccxt_history()
        {
            var results = _integration.GetCcxtHistory();
            results.Dump();
        }

        [TestMethod]
        public void Bitz__get_withdrawal_fees()
        {
            var results = _integration.GetWithdrawalFees(CachePolicy.ForceRefresh);
            results.Dump();
        }
    }
}
