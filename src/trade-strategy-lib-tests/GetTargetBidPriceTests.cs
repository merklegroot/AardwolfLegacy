using dump_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Collections.Generic;
using trade_model;
using trade_strategy_lib;

namespace trade_strategy_lib_tests
{
    [TestClass]
    public class AutoOpenBidTests
    {
        private AutoOpenBid _autoOpenBid;

        [TestInitialize]
        public void Setup()
        {
            _autoOpenBid = new AutoOpenBid();
        }

        [TestMethod]
        public void Auto_open_bid____empty_order_books()
        {
            var sourceOrderBook = new OrderBook();
            var binanceOrderBook = new OrderBook();
            var result = _autoOpenBid.ExecuteAgainstHighVolumeExchange(sourceOrderBook, binanceOrderBook);

            result.Dump();
            result.ShouldBeNull();
        }

        [TestMethod]
        public void Auto_open_bid____simple_scenario()
        {
            var sourceOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 20, Quantity = 1 }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 5, Quantity = 1 }
                }
            };

            var binanceOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 25, Quantity = 1 }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 10, Quantity = 1 }
                }
            };

            var result = _autoOpenBid.ExecuteAgainstHighVolumeExchange(sourceOrderBook, binanceOrderBook);

            result.Dump();
            result.ShouldNotBeNull();

            result.Value.ShouldBe(9);
        }

        [TestMethod]
        public void Auto_open_bid____sourcec_bid_is_above_ideal_bid()
        {
            var sourceOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 20, Quantity = 1 }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 9.001m, Quantity = 1 }
                }
            };

            var binanceOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 25, Quantity = 1 }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 10, Quantity = 1 }
                }
            };

            var result = _autoOpenBid.ExecuteAgainstHighVolumeExchange(sourceOrderBook, binanceOrderBook);

            result.Dump();
            result.ShouldNotBeNull();

            result.Value.ShouldBe(9.05m);
        }

        [TestMethod]
        public void Auto_open_bid___against_regular_exchanges()
        {
            var sourceOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 20, Quantity = 1 }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 5m, Quantity = 1 }
                }
            };

            var kucoinOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 20, Quantity = 1 }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 8m, Quantity = 1 }
                }
            };

            var hitBtcOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 20, Quantity = 1 }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 9m, Quantity = 1 }
                }
            };

            const decimal CryptoComparePrice = 7.5m;

            var result = _autoOpenBid.ExecuteAgainstRegularExchanges(sourceOrderBook, new List<OrderBook> { kucoinOrderBook, hitBtcOrderBook }, CryptoComparePrice);
            result.Dump();

            result.ShouldNotBeNull();
            result.ShouldBe(6.75m);
        }
    }
}
