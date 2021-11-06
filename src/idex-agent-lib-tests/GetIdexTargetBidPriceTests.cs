//using System;
//using System.Collections.Generic;
//using dump_lib;
//using idex_agent_lib;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Shouldly;
//using trade_model;

//namespace idex_agent_lib_tests
//{
//    [TestClass]
//    public class GetIdexTargetBidPriceTests
//    {
//        private GetIdexTargetBidPrice _getIdexBidPrice;

//        [TestInitialize]
//        public void Setup()
//        {
//            _getIdexBidPrice = new GetIdexTargetBidPrice();
//        }

//        [TestMethod]
//        public void Idex__get_target_bid_price__empty_order_books()
//        {
//            var idexOrderBook = new OrderBook();
//            var binanceOrderBook = new OrderBook();
//            var result = _getIdexBidPrice.ExecuteAgainstHighVolumeExchange(idexOrderBook, binanceOrderBook);

//            result.Dump();

//            result.ShouldBeNull();
//        }

//        [TestMethod]
//        public void Idex__get_target_bid_price__simple_scenario()
//        {
//            var idexOrderBook = new OrderBook
//            {
//                Asks = new List<Order>
//                {
//                    new Order { Price = 20, Quantity = 1 }
//                },
//                Bids = new List<Order>
//                {
//                    new Order { Price = 5, Quantity = 1 }
//                }
//            };

//            var binanceOrderBook = new OrderBook
//            {
//                Asks = new List<Order>
//                {
//                    new Order { Price = 25, Quantity = 1 }
//                },
//                Bids = new List<Order>
//                {
//                    new Order { Price = 10, Quantity = 1 }
//                }
//            };

//            var result = _getIdexBidPrice.ExecuteAgainstHighVolumeExchange(idexOrderBook, binanceOrderBook);

//            result.Dump();
//            result.ShouldNotBeNull();

//            result.Value.ShouldBe(9);
//        }

//        [TestMethod]
//        public void Idex__get_target_bid_price__idex_bid_is_above_ideal_bid()
//        {
//            var idexOrderBook = new OrderBook
//            {
//                Asks = new List<Order>
//                {
//                    new Order { Price = 20, Quantity = 1 }
//                },
//                Bids = new List<Order>
//                {
//                    new Order { Price = 9.001m, Quantity = 1 }
//                }
//            };

//            var binanceOrderBook = new OrderBook
//            {
//                Asks = new List<Order>
//                {
//                    new Order { Price = 25, Quantity = 1 }
//                },
//                Bids = new List<Order>
//                {
//                    new Order { Price = 10, Quantity = 1 }
//                }
//            };

//            var result = _getIdexBidPrice.ExecuteAgainstHighVolumeExchange(idexOrderBook, binanceOrderBook);

//            result.Dump();
//            result.ShouldNotBeNull();

//            result.Value.ShouldBe(9.05m);
//        }

//        [TestMethod]
//        public void Idex__get_target_bid_price_against_regular_exchanges()
//        {
//            var idexOrderBook = new OrderBook
//            {
//                Asks = new List<Order>
//                {
//                    new Order { Price = 20, Quantity = 1 }
//                },
//                Bids = new List<Order>
//                {
//                    new Order { Price = 5m, Quantity = 1 }
//                }
//            };

//            var kucoinOrderBook = new OrderBook
//            {
//                Asks = new List<Order>
//                {
//                    new Order { Price = 20, Quantity = 1 }
//                },
//                Bids = new List<Order>
//                {
//                    new Order { Price = 8m, Quantity = 1 }
//                }
//            };

//            var hitBtcOrderBook = new OrderBook
//            {
//                Asks = new List<Order>
//                {
//                    new Order { Price = 20, Quantity = 1 }
//                },
//                Bids = new List<Order>
//                {
//                    new Order { Price = 9m, Quantity = 1 }
//                }
//            };

//            const decimal CryptoComparePrice = 7.5m;

//            var result = _getIdexBidPrice.ExecuteAgainstRegularExchanges(idexOrderBook, new List<OrderBook> { kucoinOrderBook, hitBtcOrderBook }, CryptoComparePrice);
//            result.Dump();

//            result.ShouldNotBeNull();
//            result.ShouldBe(6.75m);
//        }
//    }
//}
