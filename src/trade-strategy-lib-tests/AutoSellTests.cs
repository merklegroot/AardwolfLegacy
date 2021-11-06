using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Collections.Generic;
using trade_browser_lib;
using trade_model;

namespace trade_strategy_lib_tests
{
    [TestClass]
    public class AutoSellTests
    {
        private AutoSellStrategy _autoSell;

        private const decimal MinimumTradeEth = 0.00251m;
        private const decimal MinimumTradeBtc = 0.00011m;

        [TestInitialize]
        public void Setup()
        {
            _autoSell = new AutoSellStrategy();
        }

        /// <summary>
        /// If you don't own anything, than you can't sell anything.
        /// </summary>
        [TestMethod]
        public void Auto_sell__nothing_owned()
        {
            var owned = 0;
            var lowVolumeEthOrderBook = new OrderBook { };
            var highVolumeEthOrderBook = new OrderBook { };

            var lowVolumeBtcOrderBook = new OrderBook { };
            var highVolumeBtcOrderBook = new OrderBook { };

            var result = _autoSell.ExecuteWithMultipleBaseSymbols(
                owned,
                lowVolumeEthOrderBook,
                highVolumeEthOrderBook,
                MinimumTradeEth,
                lowVolumeBtcOrderBook,
                highVolumeBtcOrderBook,
                MinimumTradeBtc);

            result.BtcQuantityAndPrice.ShouldBeNull();
            result.EthQuantityAndPrice.ShouldBeNull();
        }

        [TestMethod]
        public void Auto_sell__single_profitable_eth_order()
        {
            var owned = 1;
            var lowVolumeEthOrderBook = new OrderBook
            {
                Bids = new List<Order> { new Order { Price = 3, Quantity = 1 } }
            };

            var highVolumeEthOrderBook = new OrderBook
            {
                Asks = new List<Order> { new Order { Price = 2, Quantity = 1 } }
            };

            var lowVolumeBtcOrderBook = new OrderBook { };
            var highVolumeBtcOrderBook = new OrderBook { };

            var result = _autoSell.ExecuteWithMultipleBaseSymbols(
                owned,
                lowVolumeEthOrderBook,
                highVolumeEthOrderBook,
                MinimumTradeEth,
                lowVolumeBtcOrderBook,
                highVolumeBtcOrderBook,
                MinimumTradeBtc);

            result.BtcQuantityAndPrice.ShouldBeNull();
            result.EthQuantityAndPrice.Quantity.ShouldBe(1);
            result.EthQuantityAndPrice.Price.ShouldBe(3);
        }

        [TestMethod]
        public void Auto_sell__not_enough_owned_to_take_the_full_order()
        {
            var owned = 2.5m;
            var lowVolumeEthOrderBook = new OrderBook
            {
                Bids = new List<Order> { new Order { Price = 3, Quantity = 5 } }
            };

            var highVolumeEthOrderBook = new OrderBook
            {
                Asks = new List<Order> { new Order { Price = 2, Quantity = 1 } }
            };

            var lowVolumeBtcOrderBook = new OrderBook { };
            var highVolumeBtcOrderBook = new OrderBook { };

            var result = _autoSell.ExecuteWithMultipleBaseSymbols(
                owned,
                lowVolumeEthOrderBook,
                highVolumeEthOrderBook,
                MinimumTradeEth,
                lowVolumeBtcOrderBook,
                highVolumeBtcOrderBook,
                MinimumTradeBtc);

            result.BtcQuantityAndPrice.ShouldBeNull();
            result.EthQuantityAndPrice.Quantity.ShouldBe(2.5m);
            result.EthQuantityAndPrice.Price.ShouldBe(3);
        }

        [TestMethod]
        public void Auto_sell__not_profitable_single_eth_order()
        {
            var owned = 1;
            var lowVolumeEthOrderBook = new OrderBook
            {
                Bids = new List<Order> { new Order { Price = 1, Quantity = 1 } }
            };

            var highVolumeEthOrderBook = new OrderBook
            {
                Asks = new List<Order> { new Order { Price = 2, Quantity = 1 } }
            };

            var lowVolumeBtcOrderBook = new OrderBook { };
            var highVolumeBtcOrderBook = new OrderBook { };

            var result = _autoSell.ExecuteWithMultipleBaseSymbols(
                owned,
                lowVolumeEthOrderBook,
                highVolumeEthOrderBook,
                MinimumTradeEth,
                lowVolumeBtcOrderBook,
                highVolumeBtcOrderBook,
                MinimumTradeBtc);

            result.EthQuantityAndPrice.ShouldBeNull();
            result.BtcQuantityAndPrice.ShouldBeNull();
        }

        [TestMethod]
        public void Auto_sell__multiple_bids__only_one_is_profitable()
        {
            var owned = 1;
            var lowVolumeEthOrderBook = new OrderBook
            {
                Bids = new List<Order> { new Order { Price = 1, Quantity = 1 }, new Order { Price = 3, Quantity = 1 } }
            };

            var highVolumeEthOrderBook = new OrderBook
            {
                Asks = new List<Order> { new Order { Price = 2, Quantity = 1 } }
            };

            var lowVolumeBtcOrderBook = new OrderBook { };
            var highVolumeBtcOrderBook = new OrderBook { };

            var result = _autoSell.ExecuteWithMultipleBaseSymbols(
                owned,
                lowVolumeEthOrderBook,
                highVolumeEthOrderBook,
                MinimumTradeEth,
                lowVolumeBtcOrderBook,
                highVolumeBtcOrderBook,
                MinimumTradeBtc);

            result.EthQuantityAndPrice.Quantity.ShouldBe(1);
            result.EthQuantityAndPrice.Price.ShouldBe(3);
            result.BtcQuantityAndPrice.ShouldBeNull();
        }

        [TestMethod]
        public void Auto_sell__single_profitable_btc_order()
        {
            var owned = 1;

            var lowVolumeEthOrderBook = new OrderBook { };
            var highVolumeEthOrderBook = new OrderBook { };

            var lowVolumeBtcOrderBook = new OrderBook
            {
                Bids = new List<Order> { new Order { Price = 3, Quantity = 1 } }
            };

            var highVolumeBtcOrderBook = new OrderBook
            {
                Asks = new List<Order> { new Order { Price = 2, Quantity = 1 } }
            };
            
            var result = _autoSell.ExecuteWithMultipleBaseSymbols(
                owned,
                lowVolumeEthOrderBook,
                highVolumeEthOrderBook,
                MinimumTradeEth,
                lowVolumeBtcOrderBook,
                highVolumeBtcOrderBook,
                MinimumTradeBtc);

            result.EthQuantityAndPrice.ShouldBeNull();
            result.BtcQuantityAndPrice.Quantity.ShouldBe(1);
            result.BtcQuantityAndPrice.Price.ShouldBe(3);
        }

        [TestMethod]
        public void Auto_sell__eth_and_btc_single_order_each()
        {
            var owned = 2;

            var lowVolumeEthOrderBook = new OrderBook
            {
                Bids = new List<Order> { new Order { Price = 6, Quantity = 1 } }
            };

            var highVolumeEthOrderBook = new OrderBook
            {
                Asks = new List<Order> { new Order { Price = 5, Quantity = 1 } }
            };

            var lowVolumeBtcOrderBook = new OrderBook
            {
                Bids = new List<Order> { new Order { Price = 4, Quantity = 1 } }
            };

            var highVolumeBtcOrderBook = new OrderBook
            {
                Asks = new List<Order> { new Order { Price = 3, Quantity = 1 } }
            };

            var result = _autoSell.ExecuteWithMultipleBaseSymbols(
                owned,
                lowVolumeEthOrderBook,
                highVolumeEthOrderBook,
                MinimumTradeEth,
                lowVolumeBtcOrderBook,
                highVolumeBtcOrderBook,
                MinimumTradeBtc);

            result.EthQuantityAndPrice.ShouldNotBeNull();
            result.EthQuantityAndPrice.Quantity.ShouldBe(1);
            result.EthQuantityAndPrice.Price.ShouldBe(6);

            result.BtcQuantityAndPrice.ShouldNotBeNull();
            result.BtcQuantityAndPrice.Quantity.ShouldBe(1);
            result.BtcQuantityAndPrice.Price.ShouldBe(4);
        }


        [TestMethod]
        public void Auto_sell__mutiple_eth_btc_orders__only_some_are_profitable()
        {
            var owned = 10;

            var lowVolumeEthOrderBook = new OrderBook
            {
                Bids = new List<Order>
                {
                    new Order { Price = 2, Quantity = 12 }, // no
                    new Order { Price = 6, Quantity = 7 },  // yes (6-5)/5 = 0.2  [4th]
                    new Order { Price = 7, Quantity = 3 },  // yes (7-5)/5 = 0.4  [2nd]
                    new Order { Price = 5, Quantity = 9 },  // yes (5-5)/5 = 0    [tied for 5th]
                    new Order { Price = 3, Quantity = 1 },  // no
                },
            };

            var highVolumeEthOrderBook = new OrderBook
            {
                Asks = new List<Order> { new Order { Price = 5, Quantity = 1 } }
            };

            var lowVolumeBtcOrderBook = new OrderBook
            {
                Bids = new List<Order>
                {
                    new Order { Price = 2, Quantity = 2 },    // no
                    new Order { Price = 4, Quantity = 1.3m }, // yes (4-3)/3 = 0.33~ [3rd]
                    new Order { Price = 7, Quantity = 0 },    // yes (7-3)/3 = 1.3   [1st]
                    new Order { Price = 1, Quantity = 8 },    // no
                    new Order { Price = 3, Quantity = 2.5m }  // yes (3-3)/3 = 0     [tied for 5th]
                }
            };

            var highVolumeBtcOrderBook = new OrderBook
            {
                Asks = new List<Order> { new Order { Price = 3, Quantity = 1 } }
            };

            var result = _autoSell.ExecuteWithMultipleBaseSymbols(
                owned,
                lowVolumeEthOrderBook,
                highVolumeEthOrderBook,
                MinimumTradeEth,
                lowVolumeBtcOrderBook,
                highVolumeBtcOrderBook,
                MinimumTradeBtc);

            result.EthQuantityAndPrice.ShouldNotBeNull();
            result.EthQuantityAndPrice.Quantity.ShouldBe(8.7m);
            result.EthQuantityAndPrice.Price.ShouldBe(6);

            result.BtcQuantityAndPrice.ShouldNotBeNull();
            result.BtcQuantityAndPrice.Quantity.ShouldBe(1.3m);
            result.BtcQuantityAndPrice.Price.ShouldBe(4);
        }
    }
}
