//using System;
//using trade_browser_lib.Models;
//using trade_browser_lib.Strategy;
//using trade_model;

//namespace trade_browser_lib
//{
//    public class AutoSell
//    {
//        public QuantityAndPrice Execute(
//            decimal ownedQuantity,
//            OrderBook cossOrderBook,
//            OrderBook comparableOrderBook,
//            string baseSymbol
//            )
//        {
//            decimal minimumTrade = 0;
//            if (string.Equals(baseSymbol, "ETH", StringComparison.InvariantCultureIgnoreCase)) { minimumTrade = StrategyConstants.CossMinimumTradeEth; }
//            else if (string.Equals(baseSymbol, "BTC", StringComparison.InvariantCultureIgnoreCase)) { minimumTrade = StrategyConstants.CossMinimumTradeBtc; }
//            else { throw new ArgumentException($"Unexpected base symbol \"{baseSymbol}\""); }

//            if (ownedQuantity <= 0) { return null; }

//            var compBestAsk = comparableOrderBook.BestAsk();
//            var compBestAskPrice = compBestAsk.Price;

//            var cossBestBid = cossOrderBook.BestBid();
//            var cossBestBidPrice = cossBestBid.Price;
//            var cossBestBidQuantity = cossBestBid.Quantity;

//            if (cossBestBidPrice <= compBestAskPrice) { return null; }

//            var quantityToSell = cossBestBidQuantity <= ownedQuantity ? cossBestBidQuantity : ownedQuantity;
//            var valuetoSell = quantityToSell * cossBestBidPrice;
//            if (valuetoSell < minimumTrade)
//            {
//                var minimumQuantityToSell = minimumTrade / cossBestBidPrice;
//                if (minimumQuantityToSell > ownedQuantity) { return null; }
//                quantityToSell = minimumQuantityToSell;
//            }

//            return new QuantityAndPrice { Quantity = quantityToSell, Price = cossBestBidPrice };
//        }
//    }
//}
