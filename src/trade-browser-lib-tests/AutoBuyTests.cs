//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Shouldly;
//using System.Collections.Generic;
//using test_shared;
//using trade_browser_lib.Strategy;
//using trade_model;

//namespace trade_browser_lib_tests
//{
//    [TestClass]
//    public class AutoBuyTests
//    {
//        private AutoBuy _autoBuy;

//        [TestInitialize]
//        public void Setup()
//        {
//            _autoBuy = new AutoBuy();
//        }

//        [TestMethod]
//        public void Auto_buy__no_asks()
//        {
//            var asks = new List<Order> { };
//            const decimal BinanceBidPrice = 1;

//            var results = _autoBuy.Execute(asks, BinanceBidPrice, "ETH");
//            results.Dump();

//            results.Quantity.ShouldBe(0);
//        }

//        [TestMethod]
//        public void Auto_buy__loss()
//        {
//            var asks = new List<Order> { new Order { Price =  2, Quantity = 1 } };
//            const decimal BinanceBidPrice = 1;

//            var results = _autoBuy.Execute(asks, BinanceBidPrice, "ETH");
//            results.Dump();

//            results.Quantity.ShouldBe(0);
//        }

//        [TestMethod]
//        public void Auto_buy__break_even()
//        {
//            var asks = new List<Order> { new Order { Price = 2, Quantity = 1 } };
//            const decimal BinanceBidPrice = 2;

//            var results = _autoBuy.Execute(asks, BinanceBidPrice, "ETH");
//            results.Dump();

//            results.Quantity.ShouldBe(0);
//        }

//        [TestMethod]
//        public void Auto_buy__huge_profit()
//        {
//            var asks = new List<Order> { new Order { Price = 2, Quantity = 1 } };
//            const decimal BinanceBidPrice = 4;

//            var results = _autoBuy.Execute(asks, BinanceBidPrice, "ETH");
//            results.Dump();

//            results.Price.ShouldBe(2);
//            results.Quantity.ShouldBe(1);
//        }

//        [TestMethod]
//        public void Auto_buy__not_enough_profit()
//        {
//            var asks = new List<Order> { new Order { Price = 100, Quantity = 1 } };
//            const decimal BinanceBidPrice = 101;

//            var results = _autoBuy.Execute(asks, BinanceBidPrice, "ETH");
//            results.Dump();

//            results.Quantity.ShouldBe(0);
//        }

//        [TestMethod]
//        public void Auto_buy__just_under_the_threshold()
//        {
//            var asks = new List<Order> { new Order { Price = 100, Quantity = 1 } };
//            const decimal BinanceBidPrice = 107.4m;

//            var results = _autoBuy.Execute(asks, BinanceBidPrice, "ETH");
//            results.Dump();
            
//            results.Quantity.ShouldBe(0);
//        }

//        [TestMethod]
//        public void Auto_buy__exactly_enough_profit()
//        {
//            var asks = new List<Order> { new Order { Price = 100, Quantity = 1 } };
//            const decimal BinanceBidPrice = 107.5m;

//            var results = _autoBuy.Execute(asks, BinanceBidPrice, "ETH");
//            results.Dump();

//            results.Price.ShouldBe(100);
//            results.Quantity.ShouldBe(1);
//        }

//        [TestMethod]
//        public void Auto_buy__multiple__buy_them_all()
//        {
//            var asks = new List<Order>
//            {
//                new Order { Price = 1, Quantity = 4 },
//                new Order { Price = 2, Quantity = 8 },
//                new Order { Price = 3, Quantity = 2 },
//            };

//            const decimal BinanceBidPrice = 4;

//            var results = _autoBuy.Execute(asks, BinanceBidPrice, "ETH");
//            results.Dump();

//            results.Price.ShouldBe(3);
//            results.Quantity.ShouldBe(14);
//        }

//        [TestMethod]
//        public void Auto_buy__multiple__dont_buy_any()
//        {
//            var asks = new List<Order>
//            {
//                new Order { Price = 1, Quantity = 4 },
//                new Order { Price = 2, Quantity = 8 },
//                new Order { Price = 3, Quantity = 2 },
//            };

//            const decimal BinanceBidPrice = 1.02m;

//            var results = _autoBuy.Execute(asks, BinanceBidPrice, "ETH");
//            results.Dump();

//            results.Quantity.ShouldBe(0);
//        }

//        [TestMethod]
//        public void Auto_buy__multiple__buy_some_but_not_all()
//        {
//            var asks = new List<Order>
//            {
//                new Order { Price = 1, Quantity = 4 },
//                new Order { Price = 2, Quantity = 8 },
//                new Order { Price = 3, Quantity = 2 },
//            };

//            const decimal BinanceBidPrice = 2.5m;

//            var results = _autoBuy.Execute(asks, BinanceBidPrice, "ETH");
//            results.Dump();

//            results.Price.ShouldBe(2);
//            results.Quantity.ShouldBe(12);
//        }

//        [TestMethod]
//        public void Auto_buy__must_meet_minimum_trade__eth()
//        {

//            var asks = new List<Order>
//            {
//                new Order { Price = 0.000481m, Quantity = 0.0005123m }
//            };

//            const decimal BinanceBidPrice = 2.0m;

//            var results = _autoBuy.Execute(asks, BinanceBidPrice, "ETH");
//            results.Dump();

//            results.Price.ShouldBe(0.000481m);

//            var expectedValue = 5.218295218295219m;
//            var tol = 0.001m;
//            var expectedMin = expectedValue * (1.0m - tol);
//            var expectedMax = expectedValue * (1.0m + tol);
//            results.Quantity.ShouldBeInRange(expectedMin, expectedMax);
//        }
//    }
//}
