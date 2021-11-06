using dump_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Collections.Generic;
using trade_model;
using trade_strategy_lib;

namespace trade_strategy_lib_tests
{
    [TestClass]
    public class GetTargetAskPriceTests
    {
        private AutoOpenAsk _autoOpenAsk;

        [TestInitialize]
        public void Setup()
        {
            _autoOpenAsk = new AutoOpenAsk();
        }

        [TestMethod]
        public void Auto_open_ask__empty_order_books()
        {
            var sourceOrderBook = new OrderBook();
            var binanceOrderBook = new OrderBook();
            var result = _autoOpenAsk.ExecuteAgainstHighVolumeExchange(sourceOrderBook, binanceOrderBook);

            result.Dump();
            result.ShouldBeNull();
        }

        [TestMethod]
        public void Auto_open_ask__simple_scenario()
        {
            var sourceOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 25, Quantity = 1 }
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
                    new Order { Price = 20, Quantity = 1 }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 10, Quantity = 1 }
                }
            };

            var result = _autoOpenAsk.ExecuteAgainstHighVolumeExchange(sourceOrderBook, binanceOrderBook);
            result.Dump();

            result.ShouldNotBeNull();
            result.ShouldBe(22);
        }

        [TestMethod]
        public void Auto_open_ask__execute_against_regular_exchanges()
        {
            const decimal CryptoComparePrice = 20;

            var sourceOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 25, Quantity = 1 }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 5, Quantity = 1 }
                }
            };

            var bitzOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 20, Quantity = 1 }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 10, Quantity = 1 }
                }
            };

            var qryptosOrderBook = new OrderBook
            {
                Asks = new List<Order>
                {
                    new Order { Price = 18, Quantity = 1 }
                },
                Bids = new List<Order>
                {
                    new Order { Price = 13, Quantity = 1 }
                }
            };

            var comps = new List<OrderBook> { bitzOrderBook, qryptosOrderBook };
            
            var result = _autoOpenAsk.ExecuteAgainstRegularExchanges(
                sourceOrderBook,
                comps,
                CryptoComparePrice
                );

            result.Dump();

            result.ShouldNotBeNull();
            result.ShouldBe(22);
        }
    }
}
