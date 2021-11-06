//using System.Linq;
//using trade_model;

//namespace trade_strategy_lib
//{
//    public class GetTargetAskPrice
//    {
//        private const decimal DefaultTargetAskRatio = 1.10m;
//        private const decimal DefaultWorstAcceptableAskRatio = 1.025m;
//        private const decimal DefaultAskStep = 0.005m;

//        public decimal? Execute(
//            OrderBook sourceOrderBook,
//            OrderBook highVolumeOrderBook,
//            decimal? targetAskRatio = null,
//            decimal? worstAcceptableAskRatio = null,
//            decimal? askStep = null)
//        {
//            if (sourceOrderBook == null || sourceOrderBook.Asks == null || !sourceOrderBook.Asks.Any() || sourceOrderBook.Bids == null || !sourceOrderBook.Bids.Any()) { return null; }
//            if (highVolumeOrderBook == null || highVolumeOrderBook.Asks == null || !highVolumeOrderBook.Asks.Any() || highVolumeOrderBook.Bids == null || !highVolumeOrderBook.Bids.Any()) { return null; }

//            var effectiveTargetAskRatio = targetAskRatio ?? DefaultTargetAskRatio;
//            var effectiveWorstAcceptableAskRatio = worstAcceptableAskRatio ?? DefaultWorstAcceptableAskRatio;
//            var effectiveAskStep = askStep ?? DefaultAskStep;

//            var binanceBestAsk = highVolumeOrderBook.BestAsk();
//            if (binanceBestAsk == null) { return null; }
//            var highVolumeBestAskPrice = binanceBestAsk.Price;
//            if (highVolumeBestAskPrice <= 0) { return null; }

//            var sourceBestAsk = sourceOrderBook.BestAsk();
//            if (sourceBestAsk == null) { return null; }
//            var sourceBestAskPrice = sourceBestAsk.Price;
//            if (sourceBestAskPrice <= 0) { return null; }

//            for (var ratio = effectiveTargetAskRatio; ratio >= effectiveWorstAcceptableAskRatio; ratio -= effectiveAskStep)
//            {
//                var candidatePrice = highVolumeBestAskPrice * ratio;
//                if (candidatePrice > highVolumeBestAskPrice && candidatePrice < sourceBestAskPrice) { return candidatePrice; }
//            }

//            return null;
//        }
//    }
//}
