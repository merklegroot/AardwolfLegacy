using System;
using System.Linq;
using dump_lib;
using exchange_service_con;
using exchange_service_lib.Handlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using StructureMap;
using trade_constants;
using trade_contracts;
using trade_contracts.Messages.Exchange;
using trade_contracts.Messages.Exchange.History;
using trade_contracts.Payloads;
using trade_res;

namespace exchange_service_lib_tests
{
    [TestClass]
    public class ExchangeHandlerTests
    {
        private IExchangeHandler _exchangeHandler;

        [TestInitialize]
        public void Setup()
        {
            var container = Container.For<ExchangeServiceRegistry>();
            _exchangeHandler = container.GetInstance<IExchangeHandler>();
        }

        [TestMethod]
        public void Exchange_handler__get_order_book__hitbtc__usdt_tusd__allow_cache()
        {
            var req = new GetOrderBookRequestMessage
            {
                TradingPair = new TradingPairContract("USDT", "TUSD"),
                Exchange = IntegrationNameRes.HitBtc,
                CachePolicy = CachePolicyContract.AllowCache
            };

            var resp = _exchangeHandler.Handle(req);
            resp.Dump();
            resp.ShouldNotBeNull();
        }

        [TestMethod]
        public void Exchange_handler__get_order_book__blocktrade__kaya_eth__allow_cache()
        {
            var req = new GetOrderBookRequestMessage
            {
                TradingPair = new TradingPairContract("KAYA", "ETH"),
                Exchange = IntegrationNameRes.Blocktrade,
                CachePolicy = CachePolicyContract.AllowCache
            };

            var resp = _exchangeHandler.Handle(req);
            resp.Dump();
            resp.ShouldNotBeNull();
        }

        [TestMethod]
        public void Exchange_handler__get_aggregate_history()
        {
            var req = new GetAggregateExchangeHistoryRequestMessage
            {
                Payload = new GetAggregateExchangeHistoryRequestMessage.RequestPayload
                {
                    CachePolicy = CachePolicyContract.AllowCache
                }
            };

            var resp = _exchangeHandler.Handle(req);

            resp.Dump();
        }

        [TestMethod]
        public void Exchange_handler__get_history__coss__eth_btc()
        {
            var req = new GetHistoryForTradingPairRequestMessage
            {
                Payload = new GetHistoryForTradingPairRequestMessage.RequestPayload
                {
                    Exchange = IntegrationNameRes.Coss,
                    Symbol = "COSS",
                    BaseSymbol = "ETH",
                    CachePolicy = CachePolicyContract.AllowCache
                }
            };

            var resp = _exchangeHandler.Handle(req);

            resp.Dump();
        }

        [TestMethod]
        public void Exchange_handler__sell_npxs_on_bitz()
        {
            var shouldRun = false;
            if (!shouldRun)
            { throw new ApplicationException("This test works with real funds and must be run manually."); }

            var req = new SellLimitRequestMessage
            {
                Payload = new LimitRequestPayload
                {
                    Symbol = "NPXS",
                    BaseSymbol = "ETH",
                    Exchange = IntegrationNameRes.Bitz,
                    Price = 0.00000735m,
                    Quantity = 34334.71383417m
                }
            };

            var resp = _exchangeHandler.Handle(req);

            resp.Dump();
        }

        [TestMethod]
        public void Exchange_handler__sell_eth_btc_on_coss()
        {
            var shouldRun = false;

            if (!shouldRun)
            { throw new ApplicationException("This test works with real funds and must be run manually."); }

            var req = new SellLimitRequestMessage
            {
                Payload = new LimitRequestPayload
                {
                    Symbol = "ETH",
                    BaseSymbol = "BTC",
                    Exchange = IntegrationNameRes.Coss,
                    Price = 0.04m,
                    Quantity = 0.01m
                }
            };

            var resp = _exchangeHandler.Handle(req);

            resp.Dump();
        }

        [TestMethod]
        public void Exchange_handler__sell_eth_btc_on_blocktrade()
        {
            var shouldRun = false;

            if (!shouldRun)
            { throw new ApplicationException("This test works with real funds and must be run manually."); }

            var req = new SellLimitRequestMessage
            {
                Payload = new LimitRequestPayload
                {
                    Symbol = "ETH",
                    BaseSymbol = "BTC",
                    Exchange = IntegrationNameRes.Blocktrade,
                    Price = 0.03814m,
                    Quantity = 0.01m
                }
            };

            var resp = _exchangeHandler.Handle(req);

            resp.Dump();
        }

        [TestMethod]
        public void Exchange_handler__get_commodity_details__bchabc()
        {
            var req = new GetCommodityDetailsRequestMessage
            {
                Symbol = "BCHABC",
                CachePolicy = CachePolicyContract.OnlyUseCacheUnlessEmpty
            };

            var response = _exchangeHandler.Handle(req);
            response.Dump();
        }

        [TestMethod]
        public void Exchange_handler__get_commodity_details__coss()
        {
            var req = new GetCommodityDetailsRequestMessage
            {
                Symbol = "COSS",
                CachePolicy = CachePolicyContract.OnlyUseCacheUnlessEmpty
            };

            var response = _exchangeHandler.Handle(req);
            response.Dump();
        }

        [TestMethod]
        public void Exchange_handler__get_commodity_details__eos__allow_cache()
        {
            var req = new GetCommodityDetailsRequestMessage
            {
                Symbol = "EOS",
                CachePolicy = CachePolicyContract.AllowCache
            };

            var response = _exchangeHandler.Handle(req);
            response.Dump();
        }

        [TestMethod]
        public void Exchange_handler__get_commodity__pass__hitbtc__allow_cache()
        {
            var req = new GetDetailedCommodityForExchangeRequestMessage
            {
                Exchange = IntegrationNameRes.HitBtc,
                NativeSymbol = "PASS",
                CachePolicy = CachePolicyContract.AllowCache
            };

            var response = _exchangeHandler.Handle(req);

            response.Dump();
        }

        [TestMethod]
        public void Exchange_handler__get_commodity__wish__yobit__allow_cache()
        {
            var req = new GetDetailedCommodityForExchangeRequestMessage
            {
                Exchange = IntegrationNameRes.Yobit,
                NativeSymbol = "PASS",
                CachePolicy = CachePolicyContract.AllowCache
            };

            var response = _exchangeHandler.Handle(req);

            response.Dump();
        }

        [TestMethod]
        public void Exchange_handler__get_commodity__enj__binance__allow_cache()
        {
            var req = new GetDetailedCommodityForExchangeRequestMessage
            {
                Exchange = IntegrationNameRes.Binance,
                NativeSymbol = "ENJ",
                CachePolicy = CachePolicyContract.AllowCache
            };

            var response = _exchangeHandler.Handle(req);

            response.Dump();
        }


        [TestMethod]
        public void Exchange_handler__get_open_orders__qryptos__mitx_eth()
        {
            var req = new GetOpenOrdersForTradingPairRequestMessage
            {
                Exchange = IntegrationNameRes.Qryptos,
                Symbol = "MITX",
                BaseSymbol = "ETH",
                CachePolicy = CachePolicyContract.ForceRefresh
            };

            var response = _exchangeHandler.Handle(req);

            response.Dump();
        }

        [TestMethod]
        public void Exchange_handler__get_open_orders_for_trading_pair_v2__coss__req_btc()
        {
            var req = new GetOpenOrdersForTradingPairRequestMessageV2
            {
                Exchange = IntegrationNameRes.Coss,
                Symbol = "REQ",
                BaseSymbol = "BTC",
                CachePolicy = CachePolicyContract.ForceRefresh
            };

            var response = _exchangeHandler.Handle(req);

            response.Dump();
        }

        [TestMethod]
        public void Exchange_handler__get_commodities()
        {
            var req = new GetCommoditiesRequestMessage();
            var response = _exchangeHandler.Handle(req);

            response.Dump();
        }

        [TestMethod]
        public void Exchange_handler__get_exchanges()
        {
            var req = new GetExchangesRequestMessage();
            var response = _exchangeHandler.Handle(req);

            response.Dump();
        }

        [TestMethod]
        public void Exchange_handler__get_trading_pairs__oex__allow_cache()
        {
            var req = new GetTradingPairsForExchangeRequestMessage
            {
                Exchange = IntegrationNameRes.Oex,
                CachePolicy = CachePolicyContract.AllowCache
            };

            var response = _exchangeHandler.Handle(req);

            response.Dump();
        }

        [TestMethod]
        public void Exchange_handler__get_trading_pairs__coss__only_use_cache_unless_empty()
        {
            var req = new GetTradingPairsForExchangeRequestMessage
            {
                Exchange = IntegrationNameRes.Coss,
                CachePolicy = CachePolicyContract.OnlyUseCacheUnlessEmpty
            };

            var response = _exchangeHandler.Handle(req);

            response.Dump();
        }

        [TestMethod]
        public void Exchange_handler__get_trading_pairs__coss__coss_tusd__only_use_cache_unless_empty()
        {
            var req = new GetTradingPairsForExchangeRequestMessage
            {
                Exchange = IntegrationNameRes.Coss,
                CachePolicy = CachePolicyContract.OnlyUseCacheUnlessEmpty
            };

            var response = _exchangeHandler.Handle(req);

            var match = response.TradingPairs.SingleOrDefault(item => string.Equals(item.Symbol, "COSS", StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.BaseSymbol, "TUSD", StringComparison.InvariantCultureIgnoreCase));

            match.Dump();
        }

        [TestMethod]
        public void Exchange_handler__get_balance__qryptos__eth__force_refresh()
        {
            var req = new GetBalanceForCommodityAndExchangeRequestMessage
            {
                Symbol = "ETH",
                Exchange = IntegrationNameRes.Qryptos,
                CachePolicy = CachePolicyContract.ForceRefresh
            };

            var response = _exchangeHandler.Handle(req);

            response.Dump();
        }

        [TestMethod]
        public void Exchange_handler__get_balance__binance__tusd__force_refresh()
        {
            var req = new GetBalanceForCommodityAndExchangeRequestMessage
            {
                Symbol = "TUSD",
                Exchange = IntegrationNameRes.Binance,
                CachePolicy = CachePolicyContract.ForceRefresh
            };

            var response = _exchangeHandler.Handle(req);

            response.Dump();
        }

        [TestMethod]
        public void Exchange_handler__get_balances__coss__force_refresh()
        {
            var req = new GetBalanceRequestMessage
            {
                Exchange = IntegrationNameRes.Coss,
                CachePolicy = CachePolicyContract.ForceRefresh
            };

            var response = _exchangeHandler.Handle(req);

            response.Dump();
        }
    }
}
