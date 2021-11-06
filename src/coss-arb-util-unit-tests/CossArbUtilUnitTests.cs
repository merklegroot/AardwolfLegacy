using System.Collections.Generic;
using System.Linq;
using System.Text;
using cache_lib.Models;
using config_model;
using coss_arb_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using trade_model;
using trade_res;
using workflow_client_lib;
using config_client_lib;
using exchange_client_lib;
using System;
using trade_constants;
using dump_lib;
using res_util_lib;

namespace coss_arb_util_unit_tests
{
    [TestClass]
    public class CossArbUtilUnitTests
    {
        private Mock<IConfigClient> _configClient;
        private Mock<IExchangeClient> _exchangeClient;
        private Mock<IWorkflowClient> _workflowClient;
        private Mock<ILogRepo> _log;

        private CossArbUtil _cossArbUtil;

        private const decimal EthUsdPrice = 141.10m;
        private const decimal BtcUsdPrice = 4715.82m;

        private readonly HoldingInfo _holdingInfo = new HoldingInfo
        {
            Holdings = new List<Holding>(),
            TimeStampUtc = DateTime.UtcNow
        };

        [TestInitialize]
        public void Setup()
        {
            _configClient = new Mock<IConfigClient>();
            _configClient.Setup(mock => mock.GetCossAgentConfig())
                .Returns(() => new CossAgentConfig
                {
                    IsCossAutoTradingEnabled = true,
                    EthThreshold = 1.5m
                });

            _exchangeClient = new Mock<IExchangeClient>();
            _exchangeClient.Setup(mock => mock.GetBalances(IntegrationNameRes.Coss, It.IsAny<CachePolicy>())).Returns(_holdingInfo);
            _exchangeClient.Setup(mock => mock.GetBalance(IntegrationNameRes.Coss, It.IsAny<string>(), It.IsAny<CachePolicy>()))
                .Returns((string exchange, string symbol, CachePolicy cachePolicy) =>
                {
                    var match = _holdingInfo.Holdings.SingleOrDefault(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase));
                    return match ?? new Holding { Symbol = symbol };
                });

            _exchangeClient.Setup(mock => mock.GetOrderBook(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CachePolicy>()))
                .Returns((string exchange, string symbol, string baseSymbol, CachePolicy cachePolicy) =>
                {
                    var key = $"{exchange.ToUpper()}_{symbol.ToUpper()}_{baseSymbol.ToUpper()}";
                    return _orderBookDictionary.ContainsKey(key) ? _orderBookDictionary[key] : null;
                });

            _exchangeClient.Setup(mock => mock.GetTradingPairs(IntegrationNameRes.Coss, It.IsAny<CachePolicy>()))
                .Returns(GetCossTradingPairs);

            _exchangeClient.Setup(mock => mock.GetCommoditiesForExchange(IntegrationNameRes.Coss, It.IsAny<CachePolicy>()))
                .Returns(GetCossCommodities);

            _exchangeClient.Setup(mock => mock.GetTradingPairs(IntegrationNameRes.Binance, It.IsAny<CachePolicy>()))
                .Returns(GetBinanceTradingPairs);

            _exchangeClient.Setup(mock => mock.GetCommoditiesForExchange(IntegrationNameRes.Binance, It.IsAny<CachePolicy>()))
                .Returns(GetBinanceCommodities);

            _workflowClient = new Mock<IWorkflowClient>();
            _workflowClient.Setup(mock => mock.GetUsdValueV2("ETH", It.IsAny<CachePolicy>()))
                .Returns((EthUsdPrice, DateTime.UtcNow));

            _workflowClient.Setup(mock => mock.GetUsdValueV2("BTC", It.IsAny<CachePolicy>()))
                .Returns((BtcUsdPrice, DateTime.UtcNow));

            _log = new Mock<ILogRepo>();

            _cossArbUtil = new CossArbUtil(_configClient.Object, _exchangeClient.Object, _workflowClient.Object, _log.Object);
        }

        [TestMethod]
        public void Coss_arb_util_unit_tests__auto_eth_btc__sell_single_order()
        {
            var buyLimitOrders = new List<(string Exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice)>();

            _exchangeClient.Setup(mock => mock.BuyLimit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QuantityAndPrice>()))
                .Callback<string, string, string, QuantityAndPrice>((exchange, symbol, baseSymbol, quantityAndPrice) =>
                {
                    buyLimitOrders.Add((exchange, symbol, baseSymbol, quantityAndPrice));
                });

            var sellLimitOrders = new List<(string Exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice)>();

            _exchangeClient.Setup(mock => mock.SellLimit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QuantityAndPrice>()))
                .Callback<string, string, string, QuantityAndPrice>((exchange, symbol, baseSymbol, quantityAndPrice) =>
                {
                    sellLimitOrders.Add((exchange, symbol, baseSymbol, quantityAndPrice));
                });

            var binanceOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 0.03154m, Quantity = 51.29m },
                },
                Bids = new List<Order>
                {
                    new Order { Price = 0.031481m, Quantity = 6.236m },
                }
            };

            var cossOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 0.03425m, Quantity = 3.79856558m },

                },
                Bids = new List<Order>
                {
                    new Order { Price = 0.033m, Quantity = 0.00068837m },
                }
            };

            _exchangeClient.Setup(mock => mock.GetOpenOrders(IntegrationNameRes.Coss, It.IsAny<CachePolicy>()))
                .Returns(() => new List<OpenOrderForTradingPair>());

            _exchangeClient.Setup(mock => mock.GetOrderBook(IntegrationNameRes.Binance, "ETH", "BTC", It.IsAny<CachePolicy>()))
                .Returns(() => binanceOrderBook);

            _exchangeClient.Setup(mock => mock.GetOrderBook(IntegrationNameRes.Coss, "ETH", "BTC", It.IsAny<CachePolicy>()))
                .Returns(() => cossOrderBook);

            _cossArbUtil.AutoEthBtc();

            buyLimitOrders.Count().ShouldBe(0);
            sellLimitOrders.Count().ShouldBe(1);
        }

        [TestMethod]
        public void Coss_arb_util_unit_tests__auto_eth_btc__buy_single_order()
        {
            var buyLimitOrders = new List<(string Exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice)>();

            _exchangeClient.Setup(mock => mock.BuyLimit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QuantityAndPrice>()))
                .Callback<string, string, string, QuantityAndPrice>((exchange, symbol, baseSymbol, quantityAndPrice) =>
                {
                    buyLimitOrders.Add((exchange, symbol, baseSymbol, quantityAndPrice));
                });

            var sellLimitOrders = new List<(string Exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice)>();

            _exchangeClient.Setup(mock => mock.SellLimit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QuantityAndPrice>()))
                .Callback<string, string, string, QuantityAndPrice>((exchange, symbol, baseSymbol, quantityAndPrice) =>
                {
                    sellLimitOrders.Add((exchange, symbol, baseSymbol, quantityAndPrice));
                });

            var binanceOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 0.03154m, Quantity = 51.29m },
                },
                Bids = new List<Order>
                {
                    new Order { Price = 0.031481m, Quantity = 6.236m },
                }
            };

            var cossOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 0.03m, Quantity = 3.79856558m },

                },
                Bids = new List<Order>
                {
                    new Order { Price = 0.02m, Quantity = 0.00068837m },
                }
            };

            _exchangeClient.Setup(mock => mock.GetOpenOrders(IntegrationNameRes.Coss, It.IsAny<CachePolicy>()))
                .Returns(() => new List<OpenOrderForTradingPair>());

            _exchangeClient.Setup(mock => mock.GetOrderBook(IntegrationNameRes.Binance, "ETH", "BTC", It.IsAny<CachePolicy>()))
                .Returns(() => binanceOrderBook);

            _exchangeClient.Setup(mock => mock.GetOrderBook(IntegrationNameRes.Coss, "ETH", "BTC", It.IsAny<CachePolicy>()))
                .Returns(() => cossOrderBook);

            _cossArbUtil.AutoEthBtc();

            buyLimitOrders.Count().ShouldBe(1);
            sellLimitOrders.Count().ShouldBe(0);
        }

        public class LimitOrder
        {
            public string Exchange { get; set; }
            public string Symbol { get; set; }
            public string BaseSymbol { get; set; }
            public decimal Quantity { get; set; }
            public decimal Price { get; set; }
        }

        [TestMethod]
        public void Coss_arb_util_unit_tests__acquire_coss_v2()
        {
            var limitBuys = new List<LimitOrder>();
            var limitSells = new List<LimitOrder>();

            var orders = new
            {
                LimitBuys = limitBuys,
                LimitSells = limitSells
            };

            _exchangeClient.Setup(mock => mock.BuyLimit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QuantityAndPrice>()))
                .Returns(true)
                .Callback((string exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice) =>
                {
                    limitBuys.Add(new LimitOrder { Exchange = exchange, Symbol = symbol, BaseSymbol = baseSymbol, Quantity = quantityAndPrice.Quantity, Price = quantityAndPrice.Price });
                });

            _exchangeClient.Setup(mock => mock.SellLimit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QuantityAndPrice>()))
                .Returns(true)
                .Callback((string exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice) =>
                {
                    limitSells.Add(new LimitOrder { Exchange = exchange, Symbol = symbol, BaseSymbol = baseSymbol, Quantity = quantityAndPrice.Quantity, Price = quantityAndPrice.Price });
                });

            SetBalance("ETH", 10);
            SetBalance("BTC", 1);
            SetBalance("COSS", 2000);
            SetBalance("BCHABC", 0.15m);
            SetBalance("TUSD", 10000.0m);

            SetOrderBook(IntegrationNameRes.Coss, "COSS", "ETH",
                new OrderBook
                {
                    Asks = new List<Order>
                    { new Order { Price = 0.00040537m, Quantity = 250.0m }, },
                    Bids = new List<Order>
                    { new Order { Price = 0.0003953m, Quantity = 546.84176575m }, }
                });

            SetOrderBook(IntegrationNameRes.Coss, "COSS", "BTC",
            new OrderBook
            {
                Asks = new List<Order>
                { new Order { Price = 0.0000123m, Quantity = 610.87886179m }, },
                Bids = new List<Order>
                { new Order { Price = 0.00001145m, Quantity = 4182.0m }, }
            });

            SetOrderBook(IntegrationNameRes.Coss, "COSS", "BCHABC",
            new OrderBook
            {
                Asks = new List<Order>
                { new Order { Price = 0.0003899m, Quantity = 2883.1712234m }, },
                Bids = new List<Order>
                { new Order { Price = 0.00016m, Quantity = 691.7460625m }, }
            });


            SetOrderBook(IntegrationNameRes.Coss, "COSS", "TUSD",
                new OrderBook
                {
                    Asks = new List<Order>
                    { new Order { Price = 0.061m, Quantity = 11025.0m }, },
                    Bids = new List<Order>
                    { new Order { Price = 0.055755m, Quantity = 11025.0m }, }
                });

            //--- Binance

            SetOrderBook(IntegrationNameRes.Binance, "ETH", "BTC",
                new OrderBook
                {
                    Asks = new List<Order>
                    { new Order { Price = 0.02981m, Quantity = 2.732m }, },
                    Bids = new List<Order>
                    { new Order { Price = 0.029803m, Quantity = 0.068m }, }
                });

            SetOrderBook(IntegrationNameRes.Binance, "TUSD", "BTC",
                new OrderBook
                {
                    Asks = new List<Order>
                    { new Order { Price = 0.000216m, Quantity = 3872.0m }, },
                    Bids = new List<Order>
                    { new Order { Price = 0.00021547m, Quantity = 100.0m }, }
                });

            _cossArbUtil.AcquireCossV4();

            orders.Dump();
        }

        [TestMethod]
        public void Coss_arb_util_unit_tests__acquire_coss_v2__existing_high_tusd_bid()
        {
            var limitBids = new List<LimitOrder>();
            var limitAsks = new List<LimitOrder>();

            var orders = new
            {
                LimitBids = limitBids,
                LimitAsks = limitAsks
            };

            _exchangeClient.Setup(mock => mock.BuyLimit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QuantityAndPrice>()))
                .Returns(true)
                .Callback((string exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice) =>
                {
                    limitBids.Add(new LimitOrder { Exchange = exchange, Symbol = symbol, BaseSymbol = baseSymbol, Quantity = quantityAndPrice.Quantity, Price = quantityAndPrice.Price });
                });

            _exchangeClient.Setup(mock => mock.SellLimit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QuantityAndPrice>()))
                .Returns(true)
                .Callback((string exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice) =>
                {
                    limitAsks.Add(new LimitOrder { Exchange = exchange, Symbol = symbol, BaseSymbol = baseSymbol, Quantity = quantityAndPrice.Quantity, Price = quantityAndPrice.Price });
                });

            SetBalance("ETH", 10);
            SetBalance("BTC", 1);
            SetBalance("COSS", 2000);
            SetBalance("BCHABC", 0.15m);
            SetBalance("TUSD", 10000.0m);

            SetOrderBook(IntegrationNameRes.Coss, "COSS", "ETH",
                new OrderBook
                {
                    Asks = new List<Order>
                    { new Order { Price = 0.00040537m, Quantity = 250.0m }, },
                    Bids = new List<Order>
                    { new Order { Price = 0.0003953m, Quantity = 546.84176575m }, }
                });

            SetOrderBook(IntegrationNameRes.Coss, "COSS", "BTC",
            new OrderBook
            {
                Asks = new List<Order>
                { new Order { Price = 0.0000123m, Quantity = 610.87886179m }, },
                Bids = new List<Order>
                { new Order { Price = 0.00001145m, Quantity = 4182.0m }, }
            });

            SetOrderBook(IntegrationNameRes.Coss, "COSS", "BCHABC",
            new OrderBook
            {
                Asks = new List<Order>
                { new Order { Price = 0.0003899m, Quantity = 2883.1712234m }, },
                Bids = new List<Order>
                { new Order { Price = 0.00016m, Quantity = 691.7460625m }, }
            });

            SetOrderBook(IntegrationNameRes.Coss, "COSS", "TUSD",
                new OrderBook
                {
                    Asks = new List<Order>
                    { new Order { Price = 0.061m, Quantity = 11025.0m }, },
                    Bids = new List<Order>
                    {
                        new Order { Price = 0.060m, Quantity = 100.0m },
                        new Order { Price = 0.055755m, Quantity = 11025.0m },
                    }
                });

            //--- Binance

            SetOrderBook(IntegrationNameRes.Binance, "ETH", "BTC",
                new OrderBook
                {
                    Asks = new List<Order>
                    { new Order { Price = 0.02981m, Quantity = 2.732m }, },
                    Bids = new List<Order>
                    { new Order { Price = 0.029803m, Quantity = 0.068m }, }
                });

            SetOrderBook(IntegrationNameRes.Binance, "TUSD", "BTC",
                new OrderBook
                {
                    Asks = new List<Order>
                    { new Order { Price = 0.000216m, Quantity = 3872.0m }, },
                    Bids = new List<Order>
                    { new Order { Price = 0.00021547m, Quantity = 100.0m }, }
                });

            _cossArbUtil.AcquireCossV4();

            orders.Dump();

            var cossTusdBids = orders.LimitBids.Where(item => string.Equals(item.BaseSymbol, "TUSD", StringComparison.InvariantCultureIgnoreCase)).ToList();
            cossTusdBids.ShouldBeEmpty();

            var cossTusdAsk = orders.LimitAsks.Single(item => string.Equals(item.BaseSymbol, "TUSD", StringComparison.InvariantCultureIgnoreCase));
            cossTusdAsk.Price.ShouldBe(0.060m);
            cossTusdAsk.Quantity.ShouldBe(100.0m);
        }

        [TestMethod]
        public void Coss_arb_util_unit_tests__acquire_coss_dash_v2()
        {
            var cossOpenOrders = new List<OpenOrderForTradingPair>
            {
                new OpenOrderForTradingPair
                {
                    Symbol = "DASH", BaseSymbol = "BTC",
                    OrderType = OrderType.Bid,
                    Price = 0.02400001m, Quantity = 0.07168455m,
                    OrderId = "C239E1D1-16ED-49AA-B02A-530A984B34EB"
                },
                new OpenOrderForTradingPair
                {
                    Symbol = "DASH", BaseSymbol = "COSS",
                    OrderType = OrderType.Ask,
                    Price = 1536.98999997m, Quantity = 30.0m,
                    OrderId = "3F5D85FB-502E-49D3-B83D-140E8445A80E"
                },
                new OpenOrderForTradingPair
                {
                    Symbol = "DASH", BaseSymbol = "COSS",
                    OrderType = OrderType.Bid,
                    Price = 1375.00000001m, Quantity = 30.0m,
                    OrderId = "A8250965-014E-4DF2-A574-D934C8466784"
                },
                new OpenOrderForTradingPair
                {
                    Symbol = "DASH", BaseSymbol = "TUSD",
                    OrderType = OrderType.Bid,
                    Price = 150.00000001m, Quantity = 0.66666666m,
                    OrderId = "F29D420F-7187-40DB-9193-5E364547C5CC"
                }
            };

            _exchangeClient.Setup(mock => mock.GetOpenOrders(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CachePolicy>()))
                .Returns((string exchange, string symbol, string baseSymbol, CachePolicy cachePolicy) =>
                {
                    if (string.Equals(exchange, IntegrationNameRes.Coss, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return cossOpenOrders.Where(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                            && string.Equals(item.BaseSymbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase))
                            .ToList();
                    }

                    return new List<OpenOrderForTradingPair>();
                });

            var cancelledOrders = new List<dynamic>();
            _exchangeClient.Setup(mock => mock.CancelOrder(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string exchange, string orderId) =>
            {
                var didWeAddTheCancelledOrderToTheList = false;
                if (string.Equals(exchange, IntegrationNameRes.Coss, StringComparison.InvariantCultureIgnoreCase))
                {
                    var matchingOpenOrder = cossOpenOrders.Where(item => item.OrderId == orderId).FirstOrDefault();
                    if (matchingOpenOrder != null)
                    {
                        var orderBook = _exchangeClient.Object.GetOrderBook(exchange, matchingOpenOrder.Symbol, matchingOpenOrder.BaseSymbol, CachePolicy.ForceRefresh);                        
                        var matchingOrderBookOrder = (matchingOpenOrder.OrderType == OrderType.Bid ? orderBook.Bids : orderBook.Asks)
                            .Where(item => item.Price == matchingOpenOrder.Price && item.Quantity == matchingOpenOrder.Quantity)
                            .FirstOrDefault();

                        if (matchingOrderBookOrder != null)
                        {
                            (matchingOpenOrder.OrderType == OrderType.Bid ? orderBook.Bids : orderBook.Asks).Remove(matchingOrderBookOrder);
                        }

                        cossOpenOrders.Remove(matchingOpenOrder);

                        cancelledOrders.Add(new { Exchange = exchange, OrderId = orderId, Symbol = matchingOpenOrder.Symbol, BaseSymbol = matchingOpenOrder.BaseSymbol, Price = matchingOpenOrder.Price, Quantity = matchingOpenOrder.Quantity, OrderType = matchingOpenOrder.OrderTypeText });
                        didWeAddTheCancelledOrderToTheList = true;
                    }                    
                }

                if (!didWeAddTheCancelledOrderToTheList)
                {
                    cancelledOrders.Add(new { Exchange = exchange, OrderId = orderId });
                }
            });

            var limitBuys = new List<LimitOrder>();
            var limitSells = new List<LimitOrder>();

            var orders = new
            {
                LimitBuys = limitBuys,
                LimitSells = limitSells
            };
            
            _exchangeClient.Setup(mock => mock.BuyLimit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QuantityAndPrice>()))
                .Returns(true)
                .Callback((string exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice) =>
                {
                    limitBuys.Add(new LimitOrder { Exchange = exchange, Symbol = symbol, BaseSymbol = baseSymbol, Quantity = quantityAndPrice.Quantity, Price = quantityAndPrice.Price });
                });

            _exchangeClient.Setup(mock => mock.SellLimit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QuantityAndPrice>()))
                .Returns(true)
                .Callback((string exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice) =>
                {
                    limitSells.Add(new LimitOrder { Exchange = exchange, Symbol = symbol, BaseSymbol = baseSymbol, Quantity = quantityAndPrice.Quantity, Price = quantityAndPrice.Price });
                });

            SetBalance("ETH", 10.0m);
            SetBalance("BTC", 10.0m);
            SetBalance("COSS", 20000.0m);
            SetBalance("DASH", 10.0m);
            SetBalance("TUSD", 5000.0m);

            SetOrderBook(IntegrationNameRes.Coss, "COSS", "ETH",
                new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Price = 0.00050886m, Quantity = 17768.0694690m },
                        new Order { Price = 0.00050885m, Quantity = 250.0m }
                    },
                    Bids = new List<Order>
                    {
                        new Order { Price = 0.000502m, Quantity = 101.29579681m },
                        new Order { Price = 0.00050101m, Quantity = 9929.0m }
                    }
                });

            SetOrderBook(IntegrationNameRes.Coss, "COSS", "BTC",
                new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Price = 0.00001666m, Quantity = 19880.024609m },
                        new Order { Price = 0.00001665m, Quantity = 32243.095625m }
                    },
                    Bids = new List<Order>
                    {
                        new Order { Price = 0.00001618m, Quantity = 4634.0m },
                        new Order { Price = 0.00001617m, Quantity = 2000.0m }
                    }
                });

            SetOrderBook(IntegrationNameRes.Coss, "DASH", "BTC",
                new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Price = 0.02728948m, Quantity = 0.74406034m },
                        new Order { Price = 0.02728947m, Quantity = 1.37891795m }
                    },
                    Bids = new List<Order>
                    {
                        new Order { Price = 0.02400001m, Quantity = 0.07168455m },
                        new Order { Price = 0.024m, Quantity = 0.00025583m }
                    }
                });

            SetOrderBook(IntegrationNameRes.Coss, "DASH", "COSS",
                new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Price = 1536.98999998m, Quantity = 1.37891801m },
                        new Order { Price = 1536.98999997m, Quantity = 30.0m }
                    },
                    Bids = new List<Order>
                    {
                        new Order { Price = 1375.00000001m, Quantity = 30.0m },
                        new Order { Price = 1375.0m, Quantity = 1.1m }
                    }
                });

            SetOrderBook(IntegrationNameRes.Coss, "DASH", "TUSD",
                new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Price = 170.0m, Quantity = 1.17982167m },
                        new Order { Price = 161.0m, Quantity = 0.96880814m }
                    },
                    Bids = new List<Order>
                    {
                        new Order { Price = 150.00000001m, Quantity = 0.66666666m },
                        new Order { Price = 150.0m, Quantity = 0.0537834m }
                    }
                });

            SetOrderBook(IntegrationNameRes.Binance, "DASH", "BTC",
                new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Price = 0.025008m, Quantity = 0.266m },
                        new Order { Price = 0.025007m, Quantity = 0.459m  }
                    },
                    Bids = new List<Order>
                    {
                        new Order { Price = 0.024951m, Quantity = 2.769m },
                        new Order { Price = 0.02495m, Quantity = 0.399m }
                    }
                });

            SetOrderBook(IntegrationNameRes.Binance, "DASH", "ETH",
                new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Price = 0.7584m, Quantity = 0.052m },
                        new Order { Price = 0.75839m, Quantity = 0.126m }
                    },
                    Bids = new List<Order>
                    {
                        new Order { Price = 0.75527m, Quantity = 1.169m },
                        new Order { Price = 0.75484m, Quantity = 8.005m }
                    }
                });

            SetOrderBook(IntegrationNameRes.Binance, "TUSD", "BTC",
                new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Price = 0.00015715m, Quantity = 1810.0m },
                        new Order { Price = 0.00015714m, Quantity = 597.0m }
                    },
                    Bids = new List<Order>
                    {
                        new Order { Price = 0.000157m, Quantity = 2312.0m },
                        new Order { Price = 0.0001568m, Quantity = 17.0m }
                    }
                });

            SetOrderBook(IntegrationNameRes.Binance, "ETH", "BTC",
                new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Price = 0.03298m, Quantity = 0.286m },
                        new Order { Price = 0.032979m, Quantity = 1.468m }
                    },
                    Bids = new List<Order>
                    {
                        new Order { Price = 0.032975m, Quantity = 11.151m },
                        new Order { Price = 0.032972m, Quantity = 2.001m }
                    }
                });

            _cossArbUtil.AcquireAgainstBinanceSymbolV5("DASH");

            new { CancelledOrders = cancelledOrders }.Dump();

            orders.Dump();

            //var ethBuys = limitBuys.Where(item => string.Equals(item.BaseSymbol, "ETH")).ToList();
            //ethBuys.Any().ShouldBe(false);

            //var btcBuys = limitBuys.Where(item => string.Equals(item.BaseSymbol, "BTC")).ToList();
            //btcBuys.Any().ShouldBe(false);

            //var bchBuys = limitBuys.Where(item => string.Equals(item.BaseSymbol, "BCHABC")).ToList();
            //bchBuys.Count().ShouldBe(1);
        }

        [TestMethod]
        public void Coss_arb_util_unit_tests__acquire_coss_dash_v2__dont_cancel_good_dash_coss_ask()
        {
            var cossOpenOrders = new List<OpenOrderForTradingPair>
            {
                new OpenOrderForTradingPair
                {
                    Symbol = "DASH", BaseSymbol = "BTC",
                    OrderType = OrderType.Bid,
                    Price = 0.02400001m, Quantity = 0.07168455m,
                    OrderId = "C239E1D1-16ED-49AA-B02A-530A984B34EB"
                },
                new OpenOrderForTradingPair
                {
                    Symbol = "DASH", BaseSymbol = "COSS",
                    OrderType = OrderType.Ask,
                    Price = 1536.98999997m, Quantity = 30.0m,
                    OrderId = "3F5D85FB-502E-49D3-B83D-140E8445A80E"
                },
                new OpenOrderForTradingPair
                {
                    Symbol = "DASH", BaseSymbol = "COSS",
                    OrderType = OrderType.Bid,
                    Price = 1375.00000001m, Quantity = 30.0m,
                    OrderId = "A8250965-014E-4DF2-A574-D934C8466784"
                },
                new OpenOrderForTradingPair
                {
                    Symbol = "DASH", BaseSymbol = "TUSD",
                    OrderType = OrderType.Bid,
                    Price = 150.00000001m, Quantity = 0.66666666m,
                    OrderId = "F29D420F-7187-40DB-9193-5E364547C5CC"
                }
            };

            _exchangeClient.Setup(mock => mock.GetOpenOrders(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CachePolicy>()))
                .Returns((string exchange, string symbol, string baseSymbol, CachePolicy cachePolicy) =>
                {
                    if (string.Equals(exchange, IntegrationNameRes.Coss, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return cossOpenOrders.Where(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                            && string.Equals(item.BaseSymbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase))
                            .ToList();
                    }

                    return new List<OpenOrderForTradingPair>();
                });

            var cancelledOrders = new List<dynamic>();
            _exchangeClient.Setup(mock => mock.CancelOrder(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string exchange, string orderId) =>
                {
                    var didWeAddTheCancelledOrderToTheList = false;
                    if (string.Equals(exchange, IntegrationNameRes.Coss, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var matchingOpenOrder = cossOpenOrders.Where(item => item.OrderId == orderId).FirstOrDefault();
                        if (matchingOpenOrder != null)
                        {
                            var orderBook = _exchangeClient.Object.GetOrderBook(exchange, matchingOpenOrder.Symbol, matchingOpenOrder.BaseSymbol, CachePolicy.ForceRefresh);
                            var matchingOrderBookOrder = (matchingOpenOrder.OrderType == OrderType.Bid ? orderBook.Bids : orderBook.Asks)
                                .Where(item => item.Price == matchingOpenOrder.Price && item.Quantity == matchingOpenOrder.Quantity)
                                .FirstOrDefault();

                            if (matchingOrderBookOrder != null)
                            {
                                (matchingOpenOrder.OrderType == OrderType.Bid ? orderBook.Bids : orderBook.Asks).Remove(matchingOrderBookOrder);
                            }

                            cossOpenOrders.Remove(matchingOpenOrder);

                            cancelledOrders.Add(new { Exchange = exchange, OrderId = orderId, Symbol = matchingOpenOrder.Symbol, BaseSymbol = matchingOpenOrder.BaseSymbol, Price = matchingOpenOrder.Price, Quantity = matchingOpenOrder.Quantity, OrderType = matchingOpenOrder.OrderTypeText });
                            didWeAddTheCancelledOrderToTheList = true;
                        }
                    }

                    if (!didWeAddTheCancelledOrderToTheList)
                    {
                        cancelledOrders.Add(new { Exchange = exchange, OrderId = orderId });
                    }
                });

            var limitBuys = new List<LimitOrder>();
            var limitSells = new List<LimitOrder>();

            var orders = new
            {
                LimitBuys = limitBuys,
                LimitSells = limitSells
            };

            _exchangeClient.Setup(mock => mock.BuyLimit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QuantityAndPrice>()))
                .Returns(true)
                .Callback((string exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice) =>
                {
                    limitBuys.Add(new LimitOrder { Exchange = exchange, Symbol = symbol, BaseSymbol = baseSymbol, Quantity = quantityAndPrice.Quantity, Price = quantityAndPrice.Price });
                });

            _exchangeClient.Setup(mock => mock.SellLimit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QuantityAndPrice>()))
                .Returns(true)
                .Callback((string exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice) =>
                {
                    limitSells.Add(new LimitOrder { Exchange = exchange, Symbol = symbol, BaseSymbol = baseSymbol, Quantity = quantityAndPrice.Quantity, Price = quantityAndPrice.Price });
                });

            SetBalance("ETH", 10.0m);
            SetBalance("BTC", 10.0m);
            SetBalance("COSS", 20000.0m);
            SetBalance("DASH", 10.0m);
            SetBalance("TUSD", 5000.0m);

            SetOrderBook(IntegrationNameRes.Coss, "COSS", "ETH",
                new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Price = 0.00050886m, Quantity = 17768.0694690m },
                        new Order { Price = 0.00050885m, Quantity = 250.0m }
                    },
                    Bids = new List<Order>
                    {
                        new Order { Price = 0.000502m, Quantity = 101.29579681m },
                        new Order { Price = 0.00050101m, Quantity = 9929.0m }
                    }
                });

            SetOrderBook(IntegrationNameRes.Coss, "COSS", "BTC",
                new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Price = 0.00001666m, Quantity = 19880.024609m },
                        new Order { Price = 0.00001665m, Quantity = 32243.095625m }
                    },
                    Bids = new List<Order>
                    {
                        new Order { Price = 0.00001618m, Quantity = 4634.0m },
                        new Order { Price = 0.00001617m, Quantity = 2000.0m }
                    }
                });

            SetOrderBook(IntegrationNameRes.Coss, "DASH", "BTC",
                new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Price = 0.02728948m, Quantity = 0.74406034m },
                        new Order { Price = 0.02728947m, Quantity = 1.37891795m }
                    },
                    Bids = new List<Order>
                    {
                        new Order { Price = 0.02400001m, Quantity = 0.07168455m },
                        new Order { Price = 0.024m, Quantity = 0.00025583m }
                    }
                });

            SetOrderBook(IntegrationNameRes.Coss, "DASH", "COSS",
                new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Price = 1536.98999998m, Quantity = 1.37891801m },
                        new Order { Price = 1536.98999997m, Quantity = 30.0m }
                    },
                    Bids = new List<Order>
                    {
                        new Order { Price = 1375.00000001m, Quantity = 30.0m },
                        new Order { Price = 1375.0m, Quantity = 1.1m }
                    }
                });

            SetOrderBook(IntegrationNameRes.Coss, "DASH", "TUSD",
                new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Price = 170.0m, Quantity = 1.17982167m },
                        new Order { Price = 161.0m, Quantity = 0.96880814m }
                    },
                    Bids = new List<Order>
                    {
                        new Order { Price = 150.00000001m, Quantity = 0.66666666m },
                        new Order { Price = 150.0m, Quantity = 0.0537834m }
                    }
                });

            SetOrderBook(IntegrationNameRes.Binance, "DASH", "BTC",
                new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Price = 0.025008m, Quantity = 0.266m },
                        new Order { Price = 0.025007m, Quantity = 0.459m  }
                    },
                    Bids = new List<Order>
                    {
                        new Order { Price = 0.024951m, Quantity = 2.769m },
                        new Order { Price = 0.02495m, Quantity = 0.399m }
                    }
                });

            SetOrderBook(IntegrationNameRes.Binance, "DASH", "ETH",
                new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Price = 0.7584m, Quantity = 0.052m },
                        new Order { Price = 0.75839m, Quantity = 0.126m }
                    },
                    Bids = new List<Order>
                    {
                        new Order { Price = 0.75527m, Quantity = 1.169m },
                        new Order { Price = 0.75484m, Quantity = 8.005m }
                    }
                });

            SetOrderBook(IntegrationNameRes.Binance, "TUSD", "BTC",
                new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Price = 0.00015715m, Quantity = 1810.0m },
                        new Order { Price = 0.00015714m, Quantity = 597.0m }
                    },
                    Bids = new List<Order>
                    {
                        new Order { Price = 0.000157m, Quantity = 2312.0m },
                        new Order { Price = 0.0001568m, Quantity = 17.0m }
                    }
                });

            SetOrderBook(IntegrationNameRes.Binance, "ETH", "BTC",
                new OrderBook
                {
                    Asks = new List<Order>
                    {
                        new Order { Price = 0.03298m, Quantity = 0.286m },
                        new Order { Price = 0.032979m, Quantity = 1.468m }
                    },
                    Bids = new List<Order>
                    {
                        new Order { Price = 0.032975m, Quantity = 11.151m },
                        new Order { Price = 0.032972m, Quantity = 2.001m }
                    }
                });

            _cossArbUtil.AcquireAgainstBinanceSymbolV5("DASH");

            new { CancelledOrders = cancelledOrders }.Dump();

            orders.Dump();

            //var ethBuys = limitBuys.Where(item => string.Equals(item.BaseSymbol, "ETH")).ToList();
            //ethBuys.Any().ShouldBe(false);

            //var btcBuys = limitBuys.Where(item => string.Equals(item.BaseSymbol, "BTC")).ToList();
            //btcBuys.Any().ShouldBe(false);

            //var bchBuys = limitBuys.Where(item => string.Equals(item.BaseSymbol, "BCHABC")).ToList();
            //bchBuys.Count().ShouldBe(1);
        }

        [TestMethod]
        public void Coss_arb_util_unit_tests__auto_symbol__can__test()
        {
            const string Symbol = "CAN";
            const string CompExchange = IntegrationNameRes.KuCoin;

            _exchangeClient.Setup(mock => mock.GetTradingPairs(IntegrationNameRes.Coss, It.IsAny<CachePolicy>()))
                .Returns(new List<TradingPair> {
                    new TradingPair(Symbol, "ETH"),
                    new TradingPair(Symbol, "BTC")
                });

            _exchangeClient.Setup(mock => mock.GetTradingPairs(CompExchange, It.IsAny<CachePolicy>()))
                .Returns(new List<TradingPair> {
                    new TradingPair(Symbol, "ETH"),
                    new TradingPair(Symbol, "BTC")
                });

            _workflowClient.Setup(mock => mock.GetUsdValue("ETH", It.IsAny<CachePolicy>())).Returns(244.16000m);
            _workflowClient.Setup(mock => mock.GetUsdValue("BTC", It.IsAny<CachePolicy>())).Returns(6699.03000m);
            _workflowClient.Setup(mock => mock.GetUsdValue(Symbol, It.IsAny<CachePolicy>())).Returns(0.05318m);
            
            var cossSymbolEthOrderBook = new OrderBook
            {
                Bids = new List<Order>
                {
                    new Order { Price = 0.00021412m, Quantity = 50m },
                    new Order { Price = 0.00021275m, Quantity = 30m },
                },
                Asks = new List<Order>
                {
                    new Order { Price = 0.00019m, Quantity = 713.01938959m },
                    new Order { Price = 0.00028944m, Quantity = 20.00044914m }                    
                },
                AsOf = DateTime.UtcNow
            };
            _exchangeClient.Setup(mock => mock.GetOrderBook(IntegrationNameRes.Coss, Symbol, "ETH", It.IsAny<CachePolicy>()))
                .Returns(() => cossSymbolEthOrderBook);

            var cossSymbolBtcOrderBook = new OrderBook
            {
                Bids = new List<Order>
                {
                    new Order { Price = 0.00000775m, Quantity = 49.30064516m },
                    new Order { Price = 0.00000750m, Quantity = 250.00000000m },
                },
                Asks = new List<Order>
                {
                    new Order { Price = 0.00000958m, Quantity = 30.00000000m },
                    new Order { Price = 0.00000999m, Quantity = 105.72872873m },
                },
                AsOf = DateTime.UtcNow
            };
            _exchangeClient.Setup(mock => mock.GetOrderBook(IntegrationNameRes.Coss, Symbol, "BTC", It.IsAny<CachePolicy>()))
                .Returns(() => cossSymbolBtcOrderBook);

            var compSymbolEthOrderBook = new OrderBook
            {
                Bids = new List<Order>
                {
                    new Order { Price = 0.0002167m, Quantity = 96.5694m },
                    new Order { Price = 0.0002166m, Quantity = 437.8979m }
                },
                Asks = new List<Order>
                {
                    new Order { Price = 0.0002208m, Quantity = 236.3522m },
                    new Order { Price = 0.0002206m, Quantity = 912.6645m }
                },
                AsOf = DateTime.UtcNow
            };
            _exchangeClient.Setup(mock => mock.GetOrderBook(CompExchange, Symbol, "ETH", It.IsAny<CachePolicy>()))
                .Returns(() => compSymbolEthOrderBook);

            var compBtcOrderBook = new OrderBook
            {
                Bids = new List<Order>
                {
                    new Order { Price = 0.00000778m, Quantity = 746.2611m },
                    new Order { Price = 0.00000776m, Quantity = 427.6999m }
                },
                Asks = new List<Order>
                {
                    new Order { Price = 0.00000795m, Quantity = 4633.8749m },
                    new Order { Price = 0.00000794m, Quantity = 122.3089m }
                },
                AsOf = DateTime.UtcNow
            };
            _exchangeClient.Setup(mock => mock.GetOrderBook(CompExchange, Symbol, "BTC", It.IsAny<CachePolicy>()))
                .Returns(() => compBtcOrderBook);

            var balances = new HoldingInfo
            {
                Holdings = new List<Holding>
                {
                    new Holding { Symbol = Symbol, Available = 100 },
                    new Holding { Symbol = "ETH", Available = 10 },
                    new Holding { Symbol = "BTC", Available = 2 },
                }
            };

            var openOrders = new List<OpenOrderForTradingPair>();

            _exchangeClient.Setup(mock => mock.GetOpenOrders(IntegrationNameRes.Coss, It.IsAny<CachePolicy>()))
                .Returns(() => openOrders);

            _exchangeClient.Setup(mock => mock.GetOpenOrders(IntegrationNameRes.Coss, Symbol, "ETH", It.IsAny<CachePolicy>()))
                .Returns(() => openOrders != null
                    ? openOrders.Where(item => string.Equals(item.BaseSymbol, "ETH", StringComparison.InvariantCultureIgnoreCase)).ToList()
                    : null);

            _exchangeClient.Setup(mock => mock.GetOpenOrders(IntegrationNameRes.Coss, Symbol, "BTC", It.IsAny<CachePolicy>()))
                .Returns(() => openOrders != null
                    ? openOrders.Where(item => string.Equals(item.BaseSymbol, "BTC", StringComparison.InvariantCultureIgnoreCase)).ToList()
                    : null);

            _exchangeClient.Setup(mock => mock.GetBalances(IntegrationNameRes.Coss, It.IsAny<CachePolicy>()))
                .Returns(() => balances.Clone());

            _exchangeClient.Setup(mock => mock.BuyLimit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QuantityAndPrice>()))
                .Callback(() => throw new NotImplementedException());

            _exchangeClient.Setup(mock => mock.SellLimit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QuantityAndPrice>()))
                .Callback(() => throw new NotImplementedException());

            _cossArbUtil.AutoSymbol(Symbol, CompExchange);
        }

        private List<Order> TextToOrders(string text)
        {
            var lines = text.Replace("\r\n", "\r").Replace("\n", "\r").Replace("\t", " ").Split('\r')
                .Where(queryLine => !string.IsNullOrWhiteSpace(queryLine))
                .Select(queryLine => queryLine.Trim())
                .ToList();

            return lines.Select(queryLine =>
            {
                var pieces = queryLine.Split(' ').Where(querySegment => !string.IsNullOrWhiteSpace(querySegment))
                    .ToList();

                if (pieces.Count != 2) { return null; }

                var price = decimal.Parse(pieces[0]);
                var quantity = decimal.Parse(pieces[1]);

                return new Order { Price = price, Quantity = quantity };
            })
            .Where(queryLine => queryLine != null)
            .ToList();
        }

        private void SetBalance(string symbol, decimal available, decimal inOrders, decimal total)
        {
            var match = _holdingInfo.Holdings.SingleOrDefault(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase));
            if (match != null)
            {
                match.Available = available;
                match.InOrders = inOrders;
                match.Total = total;
                return;
            }

            _holdingInfo.Holdings.Add(new Holding
            {
                Symbol = symbol,
                Available = available,
                InOrders = inOrders,
                Total = total
            });
        }

        private void SetBalance(string symbol, decimal balance)
        {
            SetBalance(symbol, balance, 0, balance);
        }

        private Dictionary<string, OrderBook> _orderBookDictionary = new Dictionary<string, OrderBook>(StringComparer.InvariantCultureIgnoreCase);

        private void SetOrderBook(string exchange, string symbol, string baseSymbol, OrderBook orderBook)
        {
            var key = $"{exchange.ToUpper()}_{symbol.ToUpper()}_{baseSymbol.ToUpper()}";
            _orderBookDictionary[key] = orderBook;
        }

        private string CsonSerialize(List<Order> orders)
        {
            var builder = new StringBuilder();
            builder.AppendLine("new List<Order>");
            builder.AppendLine("{");
            for (var i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                var maybeComma = i + 1 != orders.Count ? "," : string.Empty;
                builder.AppendLine($"    new Order {{ Price = {order.Price}m, Quantity = {order.Quantity}m }}{maybeComma}");
            }
            builder.AppendLine("}");

            return builder.ToString();
        }

        private List<TradingPair> GetCossTradingPairs()
        {
            return ResUtil.Get<List<TradingPair>>("coss-trading-pairs.json", GetType().Assembly);
        }

        private List<CommodityForExchange> GetCossCommodities()
        {
            return ResUtil.Get<List<CommodityForExchange>>("coss-commodities.json", GetType().Assembly);
        }

        private List<TradingPair> GetBinanceTradingPairs()
        {
            return ResUtil.Get<List<TradingPair>>("binance-trading-pairs.json", GetType().Assembly);
        }

        private List<CommodityForExchange> GetBinanceCommodities()
        {
            return ResUtil.Get<List<CommodityForExchange>>("binance-commodities.json", GetType().Assembly);
        }
    }
}
