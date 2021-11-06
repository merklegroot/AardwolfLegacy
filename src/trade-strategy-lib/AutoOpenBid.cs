using System.Collections.Generic;
using System.Linq;
using trade_model;

namespace trade_strategy_lib
{
    public class AutoOpenBid
    {
        private const decimal DefaultDesiredBidRatio = 0.9m;
        private const decimal DefaultWorstAcceptableBidRatio = 0.94m;
        private const decimal DefaultBidStep = 0.005m;

        public decimal? ExecuteAgainstHighVolumeExchange(
            OrderBook sourceOrderBook,
            OrderBook highVolumeExchange,
            decimal? desiredBidRatio = null,
            decimal? worstAcceptableBidRatio = null,
            decimal? bidStep = null)
        {
            if (sourceOrderBook == null || sourceOrderBook.Asks == null || !sourceOrderBook.Asks.Any() || sourceOrderBook.Bids == null || !sourceOrderBook.Bids.Any()) { return null; }
            if (highVolumeExchange == null || highVolumeExchange.Asks == null || !highVolumeExchange.Asks.Any() || highVolumeExchange.Bids == null || !highVolumeExchange.Bids.Any()) { return null; }

            var effectiveDesiredBidRatio = desiredBidRatio ?? DefaultDesiredBidRatio;
            var effectiveWorstAcceptableBidRatio = worstAcceptableBidRatio ?? DefaultWorstAcceptableBidRatio;
            var effectiveBidStep = bidStep ?? DefaultBidStep;

            var highVolumeBestBid = highVolumeExchange.BestBid();
            if (highVolumeBestBid == null) { return null; }
            var highVolumeBestBidPrice = highVolumeBestBid.Price;
            if (highVolumeBestBidPrice <= 0) { return null; }

            var sourceBestBid = sourceOrderBook.BestBid();
            if (sourceBestBid == null) { return null; }
            var sourceBestBidPrice = sourceBestBid.Price;
            if (sourceBestBidPrice <= 0) { return null; }

            if (sourceBestBidPrice >= highVolumeBestBidPrice) { return null; }

            for (var ratio = effectiveDesiredBidRatio; ratio <= effectiveWorstAcceptableBidRatio; ratio += effectiveBidStep)
            {
                var desiredPrice = highVolumeBestBidPrice * ratio;
                if (desiredPrice > sourceBestBidPrice && desiredPrice < highVolumeBestBidPrice) { return desiredPrice; }
            }

            return null;
        }

        public decimal? ExecuteAgainstRegularExchanges(
            OrderBook sourceOrderBook,
            List<OrderBook> compOrderBooks,
            decimal cryptoComparePrice,
            decimal? desiredBidRatio = null,
            decimal? worstAcceptableBidRatio = null,
            decimal? bidStep = null)
        {
            if (sourceOrderBook == null || sourceOrderBook.Asks == null || !sourceOrderBook.Asks.Any() || sourceOrderBook.Bids == null || !sourceOrderBook.Bids.Any()) { return null; }
            if (compOrderBooks == null || compOrderBooks.Any(comp => comp.Asks == null) || compOrderBooks.Any(comp => !comp.Asks.Any()) || compOrderBooks.Any(comp => comp.Bids == null) || compOrderBooks.Any(comp => !comp.Bids.Any())) { return null; }
            if (compOrderBooks.Count < 2) { return null; }

            if (cryptoComparePrice <= 0) { return null; }

            var effectiveDesiredBidRatio = desiredBidRatio ?? DefaultDesiredBidRatio;
            var effectiveWorstAcceptableBidRatio = worstAcceptableBidRatio ?? DefaultWorstAcceptableBidRatio;
            var effectiveBidStep = bidStep ?? DefaultBidStep;

            decimal? lowestHigh = null;
            foreach (var comp in compOrderBooks)
            {
                var best = comp.BestBid();
                if (best != null)
                {
                    var bestPrice = best.Price;
                    if (!lowestHigh.HasValue || bestPrice < lowestHigh.Value)
                    {
                        lowestHigh = bestPrice;
                    }
                }
            }

            if (!lowestHigh.HasValue || lowestHigh.Value <= 0) { return null; }

            if (cryptoComparePrice < lowestHigh.Value)
            {
                lowestHigh = cryptoComparePrice;
            }

            var sourceBestBid = sourceOrderBook.BestBid();
            if (sourceBestBid == null) { return null; }
            var sourceBestBidPrice = sourceBestBid.Price;
            if (sourceBestBidPrice <= 0) { return null; }

            if (sourceBestBidPrice >= lowestHigh) { return null; }

            for (var ratio = effectiveDesiredBidRatio; ratio <= effectiveWorstAcceptableBidRatio; ratio += effectiveBidStep)
            {
                var desiredPrice = lowestHigh * ratio;
                if (desiredPrice > sourceBestBidPrice && desiredPrice < lowestHigh) { return desiredPrice; }
            }

            return null;
        }
    }
}
