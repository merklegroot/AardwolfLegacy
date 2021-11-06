//using dump_lib;
//using idex_agent_lib;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Shouldly;
//using System.Collections.Generic;
//using trade_model;

//namespace idex_agent_lib_tests
//{
//    [TestClass]
//    public class GetIdexTargetAskPriceTests
//    {
//        private GetIdexTargetAskPrice _getIdexTargetAskPrice;

//        [TestInitialize]
//        public void Setup()
//        {
//            _getIdexTargetAskPrice = new GetIdexTargetAskPrice();
//        }

//        [TestMethod]
//        public void Idex__get_target_ask_price__empty_order_books()
//        {
//            var idexOrderBook = new OrderBook();
//            var binanceOrderBook = new OrderBook();
//            var result = _getIdexTargetAskPrice.Execute(idexOrderBook, binanceOrderBook);

//            result.Dump();

//            result.ShouldBeNull();
//        }

//        [TestMethod]
//        public void Idex__get_target_ask_price__simple_scenario()
//        {
//            var idexOrderBook = new OrderBook
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

//            var result = _getIdexTargetAskPrice.Execute(idexOrderBook, binanceOrderBook);
//            result.Dump();

//            result.ShouldNotBeNull();
//            result.ShouldBe(22);
//        }
//    }
//}
