//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using trade_browser_lib;
//using trade_model;
//using Shouldly;
//using System.Collections.Generic;
//using test_shared;
//using System.Linq;

//namespace trade_browser_lib_tests
//{
//    [TestClass]
//    public class AutoEthBtcTests
//    {
//        private AutoEthBtc _autoEthBtc;

//        [TestInitialize]
//        public void Setup()
//        {
//            _autoEthBtc = new AutoEthBtc();
//        }

//        [TestMethod]
//        public void Auto_eth__no_orders()
//        {
//            var cossOrderBook = new OrderBook();
//            var binanceOrderBook = new OrderBook();

//            var ordersToPlace = _autoEthBtc.Execute(cossOrderBook, binanceOrderBook);
//            ordersToPlace.ShouldBeEmpty();
//        }

//        [TestMethod]
//        public void Auto_eth__no_winners()
//        {
//            var cossOrderBook = new OrderBook
//            {
//                Bids = new List<Order>
//                {
//                    new Order { Price = 2, Quantity = 1 }
//                },
//                Asks = new List<Order>
//                {
//                    new Order { Price = 5, Quantity = 1 }
//                }
//            };

//            var binanceOrderBook = new OrderBook
//            {
//                Bids = new List<Order>
//                {
//                    new Order { Price = 1, Quantity = 1 }
//                },
//                Asks = new List<Order>
//                {
//                    new Order { Price = 6, Quantity = 1 }
//                }
//            };

//            var ordersToPlace = _autoEthBtc.Execute(cossOrderBook, binanceOrderBook);
//            ordersToPlace.Dump();

//            ordersToPlace.ShouldBeEmpty();
//        }

//        [TestMethod]
//        public void Auto_eth__simple_buy()
//        {
//            var cossOrderBook = new OrderBook
//            {
//                Bids = new List<Order>
//                {
//                    new Order { Price = 2, Quantity = 1 }
//                },
//                Asks = new List<Order>
//                {
//                    new Order { Price = 3, Quantity = 1 }
//                }
//            };

//            var binanceOrderBook = new OrderBook
//            {
//                Bids = new List<Order>
//                {
//                    new Order { Price = 4, Quantity = 1 }
//                },
//                Asks = new List<Order>
//                {
//                    new Order { Price = 6, Quantity = 1 }
//                }
//            };

//            var ordersToPlace = _autoEthBtc.Execute(cossOrderBook, binanceOrderBook);
//            ordersToPlace.Dump();

//            var order = ordersToPlace.Single();
//            order.Price.ShouldBe(3);
//            order.OrderType.ShouldBe(OrderType.Bid);
//            order.Quantity.ShouldBe(1.00001m);
//        }

//        [TestMethod]
//        public void Auto_eth__simple_sell()
//        {
//            var cossOrderBook = new OrderBook
//            {
//                Bids = new List<Order>
//                {
//                    new Order { Price = 4, Quantity = 1 }
//                },
//                Asks = new List<Order>
//                {
//                    new Order { Price = 5, Quantity = 1 }
//                }
//            };

//            var binanceOrderBook = new OrderBook
//            {
//                Bids = new List<Order>
//                {
//                    new Order { Price = 2, Quantity = 1 }
//                },
//                Asks = new List<Order>
//                {
//                    new Order { Price = 3, Quantity = 1 }
//                }
//            };

//            var ordersToPlace = _autoEthBtc.Execute(cossOrderBook, binanceOrderBook);
//            ordersToPlace.Dump();

//            var order = ordersToPlace.Single();
//            order.Price.ShouldBe(4);
//            order.OrderType.ShouldBe(OrderType.Ask);
//            order.Quantity.ShouldBe(1.00001m);
//        }

//        [TestMethod]
//        public void Auto_eth__sell_multiple()
//        {
//            var cossOrderBook = new OrderBook
//            {
//                Bids = new List<Order>
//                {
//                    new Order { Price = 4, Quantity = 1 },
//                    new Order { Price = 4.1m, Quantity = 1 }
//                },
//                Asks = new List<Order>
//                {
//                    new Order { Price = 5, Quantity = 1 }
//                }
//            };

//            var binanceOrderBook = new OrderBook
//            {
//                Bids = new List<Order>
//                {
//                    new Order { Price = 2, Quantity = 1 }
//                },
//                Asks = new List<Order>
//                {
//                    new Order { Price = 3, Quantity = 1 }
//                }
//            };

//            var ordersToPlace = _autoEthBtc.Execute(cossOrderBook, binanceOrderBook);
//            ordersToPlace.Dump();

//            var order = ordersToPlace.Single();
//            order.Price.ShouldBe(4);
//            order.OrderType.ShouldBe(OrderType.Ask);
//            order.Quantity.ShouldBe(2.00001m);
//        }

//        [TestMethod]
//        public void Auto_eth__buy_multiple()
//        {
//            var cossOrderBook = new OrderBook
//            {
//                Bids = new List<Order>
//                {
//                    new Order { Price = 2, Quantity = 1 },
//                },
//                Asks = new List<Order>
//                {
//                    new Order { Price = 3, Quantity = 1 },
//                    new Order { Price = 3.1m, Quantity = 1 }
//                }
//            };

//            var binanceOrderBook = new OrderBook
//            {
//                Bids = new List<Order>
//                {
//                    new Order { Price = 4, Quantity = 1 }
//                },
//                Asks = new List<Order>
//                {
//                    new Order { Price = 5, Quantity = 1 }
//                }
//            };

//            var ordersToPlace = _autoEthBtc.Execute(cossOrderBook, binanceOrderBook);
//            ordersToPlace.Dump();

//            var order = ordersToPlace.Single();
//            order.Price.ShouldBe(3.1m);
//            order.OrderType.ShouldBe(OrderType.Bid);
//            order.Quantity.ShouldBe(2.00001m);
//        }
//    }
//}
