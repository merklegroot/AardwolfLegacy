using System;
using System.Collections.Generic;
using System.Linq;
using trade_model;

namespace trade_strategy_lib
{
    public class AutoOpenAsk
    {
        private const decimal DefaultTargetAskRatio = 1.10m;
        private const decimal DefaultWorstAcceptableAskRatio = 1.025m;
        private const decimal DefaultAskStep = 0.005m;

        public decimal? ExecuteAgainstHighVolumeExchange(
            OrderBook sourceOrderBook,
            OrderBook highVolumeOrderBook,
            decimal? targetAskRatio = null,
            decimal? worstAcceptableAskRatio = null,
            decimal? askStep = null)
        {
            if (sourceOrderBook == null || sourceOrderBook.Asks == null || !sourceOrderBook.Asks.Any() || sourceOrderBook.Bids == null || !sourceOrderBook.Bids.Any()) { return null; }
            if (highVolumeOrderBook == null || highVolumeOrderBook.Asks == null || !highVolumeOrderBook.Asks.Any() || highVolumeOrderBook.Bids == null || !highVolumeOrderBook.Bids.Any()) { return null; }

            var effectiveTargetAskRatio = targetAskRatio ?? DefaultTargetAskRatio;
            var effectiveWorstAcceptableAskRatio = worstAcceptableAskRatio ?? DefaultWorstAcceptableAskRatio;
            var effectiveAskStep = askStep ?? DefaultAskStep;

            var highVolumeBestAsk = highVolumeOrderBook.BestAsk();
            if (highVolumeBestAsk == null) { return null; }
            var highVolumeBestAskPrice = highVolumeBestAsk.Price;
            if (highVolumeBestAskPrice <= 0) { return null; }

            var sourceBestAsk = sourceOrderBook.BestAsk();
            if (sourceBestAsk == null) { return null; }
            var sourceBestAskPrice = sourceBestAsk.Price;
            if (sourceBestAskPrice <= 0) { return null; }

            for (var ratio = effectiveTargetAskRatio; ratio >= effectiveWorstAcceptableAskRatio; ratio -= effectiveAskStep)
            {
                var candidatePrice = highVolumeBestAskPrice * ratio;
                if (candidatePrice > highVolumeBestAskPrice && candidatePrice < sourceBestAskPrice) { return candidatePrice; }
            }

            return null;
        }

        public decimal? ExecuteAgainstRegularExchanges(
            OrderBook sourceOrderBook,
            List<OrderBook> compOrderBooks,
            decimal cryptoComparePrice,
            decimal? targetAskRatio = null,
            decimal? worstAcceptableAskRatio = null,
            decimal? askStep = null)
        {
            if (sourceOrderBook == null || sourceOrderBook.Asks == null || !sourceOrderBook.Asks.Any() || sourceOrderBook.Bids == null || !sourceOrderBook.Bids.Any()) { return null; }
            if (compOrderBooks == null || compOrderBooks.Any(comp => comp.Asks == null) || compOrderBooks.Any(comp => !comp.Asks.Any()) || compOrderBooks.Any(comp => comp.Bids == null) || compOrderBooks.Any(comp => !comp.Bids.Any())) { return null; }
            if (compOrderBooks.Count < 2) { return null; }

            if (cryptoComparePrice <= 0) { return null; }

            var effectiveTargetAskRatio = targetAskRatio ?? DefaultTargetAskRatio;
            var effectiveWorstAcceptableAskRatio = worstAcceptableAskRatio ?? DefaultWorstAcceptableAskRatio;
            var effectiveAskStep = askStep ?? DefaultAskStep;

            decimal? highestLow = null;
            foreach (var comp in compOrderBooks)
            {
                var best = comp.BestAsk();
                if (best != null)
                {
                    var bestPrice = best.Price;
                    if (!highestLow.HasValue || bestPrice > highestLow.Value)
                    {
                        highestLow = bestPrice;
                    }
                }
            }

            if (cryptoComparePrice > highestLow.Value)
            {
                highestLow = cryptoComparePrice;
            }

            var sourceBestAsk = sourceOrderBook.BestAsk();
            if (sourceBestAsk == null) { return null; }
            var sourceBestAskPrice = sourceBestAsk.Price;
            if (sourceBestAskPrice <= 0) { return null; }

            if (sourceBestAskPrice <= highestLow) { return null; }

            for (var ratio = effectiveTargetAskRatio; ratio >= effectiveWorstAcceptableAskRatio; ratio -= effectiveAskStep)
            {
                var desiredPrice = highestLow * ratio;
                if (desiredPrice < sourceBestAskPrice && desiredPrice > highestLow) { return desiredPrice; }
            }

            return null;
        }
    }
}
