using dump_lib;
using log_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OpenQA.Selenium;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using test_shared;
using trade_browser_lib;
using trade_browser_lib.Models;
using trade_model;

namespace trade_browser_lib_tests
{
    [TestClass]
    public class OrderManagerTests
    {
        private Mock<ILogRepo> _log;
        private OrderManager _orderManager;
        private const string Symbol = "ARK";
        private const string BaseSymbol = "ETH";
        private TradingPair _tradingPair = new TradingPair(Symbol, BaseSymbol);

        [TestInitialize]
        public void Setup()
        {
            _log = new Mock<ILogRepo>();

            _orderManager = new OrderManager(_log.Object);
        }

        [TestMethod]
        public void OrderManager__Dont_cancel_a_reasonable_bid()
        {
            var binanceOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 10, Quantity = 15.00000000m },
                    new Order { Price = 9, Quantity = 32.53000000m }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 3, Quantity = 6.20000000m },
                    new Order { Price = 2, Quantity = 8.30609494m }
                }
            };

            var cossOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 9.5m, Quantity = 15.00000000m },
                    new Order { Price = 9.2m, Quantity = 32.53000000m }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 2.5m, Quantity = 6.20000000m },
                    new Order { Price = 2.4m, Quantity = 8.30609494m }
                }
            };

            const decimal Price = 2.6m;
            const decimal Quantity = 4.0m;

            var cancelButton = new Mock<IWebElement>();
            var orders = new List<OpenOrderEx>
            {
                new OpenOrderEx(Price, Quantity, OrderType.Bid, cancelButton.Object)
            };

            var placedBids = new List<OpenOrder>();
            var placedAsks = new List<OpenOrder>();
            var placeBid = new Action<OpenOrder>((OpenOrder bid) => { placedBids.Add(bid); });
            var placeAsk = new Action<OpenOrder>((OpenOrder ask) => { placedAsks.Add(ask); });
            _orderManager.ManageOrders(_tradingPair, orders, cossOrderBook, binanceOrderBook, placeBid, placeAsk);

            cancelButton.Verify(mock => mock.Click(), Times.Never);
        }

        [TestMethod]
        public void OrderManager__Dont_cancel_a_reasonable_ask()
        {
            var binanceOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 10, Quantity = 15.00000000m },
                    new Order { Price = 7, Quantity = 32.53000000m }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 3, Quantity = 6.20000000m },
                    new Order { Price = 2, Quantity = 8.30609494m }
                }
            };

            var cossOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 8.7m, Quantity = 15.00000000m },
                    new Order { Price = 8.6m, Quantity = 32.53000000m }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 2.5m, Quantity = 6.20000000m },
                    new Order { Price = 2.4m, Quantity = 8.30609494m }
                }
            };

            const decimal Price = 8.5m;
            const decimal Quantity = 4.0m;

            var cancelButton = new Mock<IWebElement>();
            var orders = new List<OpenOrderEx>
            {
                new OpenOrderEx(Price, Quantity, OrderType.Ask, cancelButton.Object)
            };

            var placedBids = new List<OpenOrder>();
            var placedAsks = new List<OpenOrder>();
            var placeBid = new Action<OpenOrder>((OpenOrder bid) => { placedBids.Add(bid); });
            var placeAsk = new Action<OpenOrder>((OpenOrder ask) => { placedAsks.Add(ask); });

            _orderManager.ManageOrders(_tradingPair, orders, cossOrderBook, binanceOrderBook, placeBid, placeAsk);

            cancelButton.Verify(mock => mock.Click(), Times.Never);
        }

        [TestMethod]
        public void OrderManager__Cancel_a_bid_thats_above_binances_highest_bid()
        {
            var binanceOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 10, Quantity = 15.00000000m },
                    new Order { Price = 9, Quantity = 32.53000000m }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 3, Quantity = 6.20000000m },
                    new Order { Price = 2, Quantity = 8.30609494m }
                }
            };

            var cossOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 9.5m, Quantity = 15.00000000m },
                    new Order { Price = 9.2m, Quantity = 32.53000000m }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 2.5m, Quantity = 6.20000000m },
                    new Order { Price = 2.4m, Quantity = 8.30609494m }
                }
            };

            const decimal Price = 7.0m;
            const decimal Quantity = 4.0m;

            var cancelButton = new Mock<IWebElement>();
            var myOpenOrders = new List<OpenOrderEx>
            {
                new OpenOrderEx(Price, Quantity, OrderType.Bid, cancelButton.Object)
            };

            var placedBids = new List<OpenOrder>();
            var placedAsks = new List<OpenOrder>();
            var placeBid = new Action<OpenOrder>((OpenOrder bid) => { placedBids.Add(bid); });
            var placeAsk = new Action<OpenOrder>((OpenOrder ask) => { placedAsks.Add(ask); });

            _orderManager.ManageOrders(_tradingPair, myOpenOrders, cossOrderBook, binanceOrderBook, placeBid, placeAsk);

            cancelButton.Verify(mock => mock.Click(), Times.Exactly(1));
        }

        [TestMethod]
        public void OrderManager__Place_a_better_bid_if_its_been_outbid()
        {
            var binanceOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 10, Quantity = 15.00000000m },
                    new Order { Price = 9, Quantity = 32.53000000m }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 3, Quantity = 6.20000000m },
                    new Order { Price = 2, Quantity = 8.30609494m }
                }
            };

            var cossOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 9.5m, Quantity = 15.00000000m },
                    new Order { Price = 9.2m, Quantity = 32.53000000m }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 2.7m, Quantity = 6.20000000m },
                    new Order { Price = 2.4m, Quantity = 8.30609494m }
                }
            };

            const decimal Price = 2.6m;
            const decimal Quantity = 4.0m;

            var cancelButton = new Mock<IWebElement>();
            var orders = new List<OpenOrderEx>
            {
                new OpenOrderEx(Price, Quantity, OrderType.Bid, cancelButton.Object)
            };

            var placedBids = new List<OpenOrder>();
            var placedAsks = new List<OpenOrder>();
            var placeBid = new Action<OpenOrder>((OpenOrder bid) => { placedBids.Add(bid); });
            var placeAsk = new Action<OpenOrder>((OpenOrder ask) => { placedAsks.Add(ask); });
            _orderManager.ManageOrders(_tradingPair, orders, cossOrderBook, binanceOrderBook, placeBid, placeAsk);

            cancelButton.Verify(mock => mock.Click(), Times.Exactly(1));
            placedAsks.ShouldBeEmpty();
            var placedBid = placedBids.Single();
            placedBid.Quantity.ShouldBe(4.0m);
            placedBid.Price.ShouldBe(2.70000005m);
        }

        [TestMethod]
        public void OrderManager__Place_a_better_ask_if_its_been_outasked()
        {
            var binanceOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 10, Quantity = 15.00000000m },
                    new Order { Price = 9, Quantity = 32.53000000m }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 3, Quantity = 6.20000000m },
                    new Order { Price = 2, Quantity = 8.30609494m }
                }
            };

            var cossOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 9.7m, Quantity = 15.00000000m },
                    new Order { Price = 9.4m, Quantity = 32.53000000m }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 2.5m, Quantity = 6.20000000m },
                    new Order { Price = 2.4m, Quantity = 8.30609494m }
                }
            };

            const decimal Price = 9.5m;
            const decimal Quantity = 4.0m;

            var cancelButton = new Mock<IWebElement>();
            var myOpenOrders = new List<OpenOrderEx>
            {
                new OpenOrderEx(Price, Quantity, OrderType.Ask, cancelButton.Object)
            };

            var placedBids = new List<OpenOrder>();
            var placedAsks = new List<OpenOrder>();
            var placeBid = new Action<OpenOrder>((OpenOrder bid) => { placedBids.Add(bid); });
            var placeAsk = new Action<OpenOrder>((OpenOrder ask) => { placedAsks.Add(ask); });

            _orderManager.ManageOrders(_tradingPair, myOpenOrders, cossOrderBook, binanceOrderBook, placeBid, placeAsk);

            cancelButton.Verify(mock => mock.Click(), Times.Exactly(1));
            placedBids.ShouldBeEmpty();
            var placedAsk = placedAsks.Single();
            placedAsk.Quantity.ShouldBe(Quantity);
            placedAsk.Price.ShouldBe(9.39999995m);
        }

        [TestMethod]
        public void OrderManager__Cancel_an_ask_thats_below_binances_lowest_ask()
        {
            var binanceOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 10, Quantity = 14.50000000m },
                    new Order { Price = 9, Quantity = 32.53000000m }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 3, Quantity = 6.20000000m },
                    new Order { Price = 2, Quantity = 8.30609494m }
                }
            };

            var cossOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 9.5m, Quantity = 15.00000000m },
                    new Order { Price = 9.2m, Quantity = 32.53000000m }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 2.5m, Quantity = 6.20000000m },
                    new Order { Price = 2.4m, Quantity = 8.30609494m }
                }
            };

            const decimal Price = 8.0m;
            const decimal Quantity = 4.0m;

            var cancelButton = new Mock<IWebElement>();
            var myOpenOrders = new List<OpenOrderEx>
            {
                new OpenOrderEx(Price, Quantity, OrderType.Ask, cancelButton.Object)
            };

            var placedBids = new List<OpenOrder>();
            var placedAsks = new List<OpenOrder>();
            var placeBid = new Action<OpenOrder>((OpenOrder bid) => { placedBids.Add(bid); });
            var placeAsk = new Action<OpenOrder>((OpenOrder ask) => { placedAsks.Add(ask); });

            _orderManager.ManageOrders(_tradingPair, myOpenOrders, cossOrderBook, binanceOrderBook, placeBid, placeAsk);
            cancelButton.Verify(mock => mock.Click(), Times.Exactly(1));
        }        

        [TestMethod]
        public void OrderManager__Buy_a_commodity_thats_being_sold_for_less_than_its_worth()
        {
            var binanceOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 9, Quantity = 15.00000000m },
                    new Order { Price = 8, Quantity = 32.53000000m }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 6, Quantity = 6.20000000m },
                    new Order { Price = 4, Quantity = 8.30609494m }
                }
            };

            var cossOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 6.5m, Quantity = 15.00000000m },
                    new Order { Price = 5.0m, Quantity = 32.53000000m }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 3.0m, Quantity = 8.45m },
                    new Order { Price = 2.4m, Quantity = 4.28m }
                }
            };

            var cancelButton = new Mock<IWebElement>();
            var myOpenOrders = new List<OpenOrderEx>();

            var placedBids = new List<OpenOrder>();
            var placedAsks = new List<OpenOrder>();
            var placeBid = new Action<OpenOrder>((OpenOrder bid) => { placedBids.Add(bid); });
            var placeAsk = new Action<OpenOrder>((OpenOrder ask) => { placedAsks.Add(ask); });
            _orderManager.ManageOrders(_tradingPair, myOpenOrders, cossOrderBook, binanceOrderBook, placeBid, placeAsk);

            cancelButton.Verify(mock => mock.Click(), Times.Never);

            placedAsks.ShouldBeEmpty();
            var placedBid = placedBids.Single();
            placedBid.Dump();

            placedBid.Price.ShouldBe(5.0m);
            placedBid.Quantity.ShouldBe(32.53000000m);            
        }
    }
}
