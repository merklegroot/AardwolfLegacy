//using System;
//using System.Collections.Generic;
//using System.Linq;
//using trade_browser_lib.Models;
//using trade_model;

//namespace trade_browser_lib.Strategy
//{
//    public class AutoBuy
//    {
//        public QuantityAndPrice Execute(
//            List<Order> cossAsks,
//            decimal binancePrice,
//            string baseCommodity)
//        {
//            if (cossAsks == null || !cossAsks.Any()) { return new QuantityAndPrice { }; }

//            var minimumDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
//            {
//                { "ETH", StrategyConstants.CossMinimumTradeEth },
//                { "BTC", StrategyConstants.CossMinimumTradeEth }
//            };

//            if (!minimumDictionary.ContainsKey(baseCommodity)) { throw new ArgumentException($"Unexpected base commodity \"{baseCommodity}\"."); }

//            var minimumTrade = minimumDictionary[baseCommodity];

//            var ordersToBuy = new List<Order>();
//            foreach (var ask in cossAsks)
//            {
//                // nothing should ever be free.
//                if (ask.Price <= 0) { throw new ApplicationException($"Unexpected price \"{ask.Price}\""); }

//                // and we're not dealing with anti-matter here.
//                if (ask.Quantity < 0) { throw new ApplicationException($"Unexpected quantity \"{ask.Quantity}\""); }

//                // a zero quantity could happen though if the quantity is so low that it rounds down to 0.
//                // if that happens, just move on.
//                if (ask.Quantity == 0) { continue; }

//                var priceDifference = binancePrice - ask.Price;
//                var profitPercentage = 100.0m * priceDifference / ask.Price;

//                if (profitPercentage >= StrategyConstants.AutoBuyPercentThreshold)
//                {
//                    ordersToBuy.Add(ask);
//                }
//            }

//            if (!ordersToBuy.Any()) { return new QuantityAndPrice { Quantity = 0 }; }

//            var highestPriceToBuy = ordersToBuy.Max(item => item.Price);
//            var quantityToBuy = ordersToBuy.Sum(item => item.Quantity);

//            if (quantityToBuy * highestPriceToBuy < minimumTrade)
//            {
//                quantityToBuy = minimumTrade / highestPriceToBuy;
//            }

//            return new QuantityAndPrice { Price = highestPriceToBuy, Quantity = quantityToBuy };
//        }
//    }
//}
