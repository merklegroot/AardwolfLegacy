//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System.Collections.Generic;
//using trade_model;
//using trade_strategy_lib;
//using Shouldly;
//using System.Linq;
//using System;

//namespace trade_strategy_lib_tests
//{
//    [TestClass]
//    public class AutoOfferTests
//    {
//        private AutoOffer _autoOffer;

//        [TestInitialize]
//        public void Setup()
//        {
//            _autoOffer = new AutoOffer();
//        }

//        [TestMethod]
//        public void Auto_offer__empty_books__no_open_orders()
//        {
//            var currentBidPrices = new List<decimal>();
//            var lowVolumeOrderBook = new OrderBook();
//            var highVolumeOrderBook = new OrderBook();

//            var actions = _autoOffer.Execute(currentBidPrices, lowVolumeOrderBook, highVolumeOrderBook);
//            actions.ShouldBeEmpty();
//        }

//        // If the books are empty, something is wrong.
//        // Cancel our open orders.
//        [TestMethod]
//        public void Auto_offer__empty_books__multiple_open_orders()
//        {
//            var currentBidPrices = new List<decimal> { 1, 2, 3 };
//            var lowVolumeOrderBook = new OrderBook();
//            var highVolumeOrderBook = new OrderBook();

//            var actions = _autoOffer.Execute(currentBidPrices, lowVolumeOrderBook, highVolumeOrderBook);
//            actions.Count.ShouldBe(3);
//            foreach(var bid in currentBidPrices)
//            {
//                actions.Single(action => 
//                    action.ActionType == AutoOffer.AutoOfferAction.AutoOfferActionType.CancelBid
//                    && action.Price == bid);
//            }
//        }

//        [TestMethod]
//        public void Auto_offer__clear_opening_to_place_bid()
//        {
//            var currentBidPrices = new List<decimal>();
//            var lowVolumeOrderBook = new OrderBook
//            {
//                Bids = new List<Order> { new Order { Price = 1, Quantity = 1 } },
//                Asks = new List<Order> { new Order { Price = 4, Quantity = 1 } }
//            };

//            var highVolumeOrderBook = new OrderBook
//            {
//                Bids = new List<Order> { new Order { Price = 2, Quantity = 1 } },
//                Asks = new List<Order> { new Order { Price = 3, Quantity = 1 } }
//            };

//            var actions = _autoOffer.Execute(currentBidPrices, lowVolumeOrderBook, highVolumeOrderBook);

//            var action = actions.Single();
//            action.ActionType.ShouldBe(AutoOffer.AutoOfferAction.AutoOfferActionType.PlaceBid);
//            action.Price.ShouldBe(1.7m);
//        }

//        // TODO: Add more to this.
//        // TODO: When we have multiple bids open, keep only the lowest valid bid.
//        [TestMethod]
//        public void Auto_offer__when_we_have_multiple_bids__only_keep_the_lowest_valid_bid()
//        {
//            var currentBidPrices = new List<decimal> { 0.8m, 1.6m, 2.2m };
//            var lowVolumeOrderBook = new OrderBook
//            {
//                Bids = new List<Order> { new Order { Price = 1, Quantity = 1 } },
//                Asks = new List<Order> { new Order { Price = 4, Quantity = 1 } }
//            };

//            var highVolumeOrderBook = new OrderBook
//            {
//                Bids = new List<Order> { new Order { Price = 2, Quantity = 1 } },
//                Asks = new List<Order> { new Order { Price = 3, Quantity = 1 } }
//            };

//            var actions = _autoOffer.Execute(currentBidPrices, lowVolumeOrderBook, highVolumeOrderBook);

//            actions.Count.ShouldBe(2);
//            // When we're cancelling multiple bids, it doesn't really matter what order we cancel them in.
//            // However, cancels should still occur before other action types.
//            actions.Any(item => item.ActionType == AutoOffer.AutoOfferAction.AutoOfferActionType.CancelBid &&
//                item.Price == 0.8m).ShouldBe(true);

//            actions.Any(item => item.ActionType == AutoOffer.AutoOfferAction.AutoOfferActionType.CancelBid &&
//                item.Price == 2.2m).ShouldBe(true);
//        }

//        [TestMethod]
//        public void Auto_offer__no_room_for_a_bid()
//        {
//            var currentBidPrices = new List<decimal>();
//            var lowVolumeOrderBook = new OrderBook
//            {
//                Bids = new List<Order> { new Order { Price = 2, Quantity = 1 } },
//                Asks = new List<Order> { new Order { Price = 3, Quantity = 1 } }
//            };

//            var highVolumeOrderBook = new OrderBook
//            {
//                Bids = new List<Order> { new Order { Price = 1, Quantity = 1 } },
//                Asks = new List<Order> { new Order { Price = 4, Quantity = 1 } }
//            };

//            var actions = _autoOffer.Execute(currentBidPrices, lowVolumeOrderBook, highVolumeOrderBook);
//            actions.ShouldBeEmpty();
//        }

//        [TestMethod]
//        public void Auto_offer__cancel_bid_thats_too_high()
//        {
//            var currentBidPrices = new List<decimal> { 1.5m };
//            var lowVolumeOrderBook = new OrderBook
//            {
//                Bids = new List<Order> { new Order { Price = 2, Quantity = 1 } },
//                Asks = new List<Order> { new Order { Price = 3, Quantity = 1 } }
//            };

//            var highVolumeOrderBook = new OrderBook
//            {
//                Bids = new List<Order> { new Order { Price = 1, Quantity = 1 } },
//                Asks = new List<Order> { new Order { Price = 4, Quantity = 1 } }
//            };

//            var actions = _autoOffer.Execute(currentBidPrices, lowVolumeOrderBook, highVolumeOrderBook);
//            var action = actions.Single();

//            action.ActionType.ShouldBe(AutoOffer.AutoOfferAction.AutoOfferActionType.CancelBid);
//            action.Price.ShouldBe(1.5m);
//        }

//        [TestMethod]
//        public void Auto_offer__cancel_bid_thats_been_outbid__then_rebid()
//        {
//            var currentBidPrices = new List<decimal> { 0.5m };
//            var lowVolumeOrderBook = new OrderBook
//            {
//                Bids = new List<Order> { new Order { Price = 1, Quantity = 1 } },
//                Asks = new List<Order> { new Order { Price = 4, Quantity = 1 } }
//            };

//            var highVolumeOrderBook = new OrderBook
//            {
//                Bids = new List<Order> { new Order { Price = 2, Quantity = 1 } },
//                Asks = new List<Order> { new Order { Price = 3, Quantity = 1 } }
//            };

//            var actions = _autoOffer.Execute(currentBidPrices, lowVolumeOrderBook, highVolumeOrderBook);
//            actions.Count.ShouldBe(2);
//            actions[0].ActionType.ShouldBe(AutoOffer.AutoOfferAction.AutoOfferActionType.CancelBid);
//            actions[0].Price.ShouldBe(0.5m);

//            actions[1].ActionType.ShouldBe(AutoOffer.AutoOfferAction.AutoOfferActionType.PlaceBid);
//            actions[1].Price.ShouldBe(1.7m);
//        }
//    }
//}
