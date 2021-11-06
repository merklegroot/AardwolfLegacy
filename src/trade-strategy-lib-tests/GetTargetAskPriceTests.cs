//using dump_lib;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Shouldly;
//using System.Collections.Generic;
//using trade_model;
//using trade_strategy_lib;

//namespace idex_agent_lib_tests
//{
//    [TestClass]
//    public class GetTargetAskPriceTests
//    {
//        private GetTargetAskPrice _getTargetAskPrice;

//        [TestInitialize]
//        public void Setup()
//        {
//            _getTargetAskPrice = new GetTargetAskPrice();
//        }

//        [TestMethod]
//        public void Strategy__get_target_ask_price__empty_order_books()
//        {
//            var sourceOrderBook = new OrderBook();
//            var binanceOrderBook = new OrderBook();
//            var result = _getTargetAskPrice.Execute(sourceOrderBook, binanceOrderBook);

//            result.Dump();

//            result.ShouldBeNull();
//        }

//        [TestMethod]
//        public void Strategy__get_target_ask_price__simple_scenario()
//        {
//            var sourceOrderBook = new OrderBook
//            {
//                Asks = new List<Order>
//                {
//                    new Order { Price = 25, Quantity = 1 }
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
//                    new Order { Price = 20, Quantity = 1 }
//                },
//                Bids = new List<Order>
//                {
//                    new Order { Price = 10, Quantity = 1 }
//                }
//            };

//            var result = _getTargetAskPrice.Execute(sourceOrderBook, binanceOrderBook);
//            result.Dump();

//            result.ShouldNotBeNull();
//            result.ShouldBe(22);
//        }
//    }
//}
