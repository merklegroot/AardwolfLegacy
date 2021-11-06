//using System;
//using System.Collections.Generic;
//using System.Linq;
//using trade_model;

//namespace trade_strategy_lib
//{
//    public class AutoOffer
//    {
//        public class AutoOfferAction
//        {
//            public enum AutoOfferActionType
//            {
//                Unknown,
//                CancelBid,
//                PlaceBid
//            }

//            public AutoOfferActionType ActionType { get; set; }
//            public decimal Price { get; set; }
//        }

//        public List<AutoOfferAction> Execute(
//            List<decimal> currentBidPrices,
//            OrderBook lowVolumeOrderBook,
//            OrderBook highVolumeOrderBook)
//        {
//            var actions = new List<AutoOfferAction>();

//            // If there's something wrong with the order books, cancel our bids
//            // get out of here.
//            if (lowVolumeOrderBook?.Bids == null  || !lowVolumeOrderBook.Bids.Any()
//                || highVolumeOrderBook?.Bids == null || !highVolumeOrderBook.Bids.Any())
//            {
//                foreach(var bid in currentBidPrices ?? new List<decimal>())
//                {
//                    actions.Add(new AutoOfferAction { ActionType = AutoOfferAction.AutoOfferActionType.CancelBid, Price = bid });
//                }

//                return actions;
//            }

//            var lowVolumeBestBidPrice = lowVolumeOrderBook.BestBid().Price;
//            var highVolumeBestBidPrice = highVolumeOrderBook.BestBid().Price;

//            foreach (var currentBidPrice in currentBidPrices ?? new List<decimal>())
//            {
//                if (currentBidPrice < lowVolumeBestBidPrice || currentBidPrice > highVolumeBestBidPrice)
//                {
//                    actions.Add(new AutoOfferAction
//                    {
//                        ActionType = AutoOfferAction.AutoOfferActionType.CancelBid,
//                        Price = currentBidPrice
//                    });
//                }
//            }

//            var getBidPricesThatDontYetHaveACancel = new Func<List<decimal>>(() => currentBidPrices.Where(bid => !actions.Any(action => action.Price == bid && action.ActionType == AutoOfferAction.AutoOfferActionType.CancelBid))
//                .OrderByDescending(item => item)
//                .ToList());

//            var bidPricesThatDontYetHaveACancel = getBidPricesThatDontYetHaveACancel();

//            // skip the first one
//            for (var i = 1; i < bidPricesThatDontYetHaveACancel.Count(); i++)
//            {
//                actions.Add(new AutoOfferAction
//                {
//                    ActionType = AutoOfferAction.AutoOfferActionType.CancelBid,
//                    Price = bidPricesThatDontYetHaveACancel[i]
//                });
//            }

//            // only consider placing a bid if we're not keeping any of our existing bids.
//            if (!getBidPricesThatDontYetHaveACancel().Any())
//            {
//                var desiredBidPrice = 0.85m * highVolumeBestBidPrice;
//                if (desiredBidPrice > lowVolumeBestBidPrice)
//                {
//                    actions.Add(new AutoOfferAction
//                    {
//                        ActionType = AutoOfferAction.AutoOfferActionType.PlaceBid,
//                        Price = desiredBidPrice
//                    });
//                }
//            }

//            return actions;
//        }
//    }
//}
