using log_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using trade_model;

namespace trade_strategy_lib
{
    public class AutoBuy
    {
        private readonly ILogRepo _log;

        public AutoBuy(ILogRepo log)
        {
            _log = log;
        }

        public QuantityAndPrice Execute(
            List<Order> lowVolumeAsks,
            decimal highVolumePrice,
            decimal minimumBaseCommodityTrade,
            decimal percentThreshold,
            TradingPair tradingPair = null,
            int? lotSize = null)
        {
            if (lowVolumeAsks == null || !lowVolumeAsks.Any()) { return new QuantityAndPrice { }; }
            if (highVolumePrice <= 0) { return new QuantityAndPrice { }; }           

            var ordersToBuy = new List<Order>();
            decimal? bestProfitPercentage = null;
            foreach (var ask in lowVolumeAsks)
            {
                // nothing should ever be free.
                if (ask.Price <= 0) { throw new ApplicationException($"Unexpected price \"{ask.Price}\""); }

                // and we're not dealing with anti-matter here.
                if (ask.Quantity < 0) { throw new ApplicationException($"Unexpected quantity \"{ask.Quantity}\""); }

                // a zero quantity could happen though if the quantity is so low that it rounds down to 0.
                // if that happens, just move on.
                if (ask.Quantity == 0) { continue; }

                var priceDifference = highVolumePrice - ask.Price;
                var profitPercentage = 100.0m * priceDifference / ask.Price;
                if (!bestProfitPercentage.HasValue || profitPercentage > bestProfitPercentage.Value)
                {
                    bestProfitPercentage = profitPercentage;
                }

                if (profitPercentage >= percentThreshold)
                {
                    ordersToBuy.Add(ask);
                }
            }

            var bestProfitPercentageText = bestProfitPercentage.HasValue ? bestProfitPercentage.Value.ToString("N4") : "null";
            if (!ordersToBuy.Any())
            {
                var tradingPairText = tradingPair != null ? tradingPair.ToString() : "orders";
                var logText = $"Didn't find any {tradingPairText} worth buying. The best profit found was {bestProfitPercentageText}%. The threshold is {percentThreshold}%.";
                _log.Info(logText);
                return new QuantityAndPrice { Quantity = 0 };
            }

            var highestPriceToBuy = ordersToBuy.Max(item => item.Price);
            var quantityToBuy = ordersToBuy.Sum(item => item.Quantity);

            if (quantityToBuy * highestPriceToBuy < minimumBaseCommodityTrade)
            {
                quantityToBuy = minimumBaseCommodityTrade / highestPriceToBuy;
            }

            if(quantityToBuy > 0 && lotSize.HasValue && lotSize.Value > 0)
            {
                quantityToBuy = RoundUp(quantityToBuy, lotSize.Value);
            }

            Console.WriteLine($"Found a good order! The best profit found was {bestProfitPercentageText}%. The threshold is {percentThreshold}%.");
            return new QuantityAndPrice { Price = highestPriceToBuy, Quantity = quantityToBuy };
        }

        private static int RoundUp(decimal quantity, int lotSize)
        {
            var result = ((int)quantity) / lotSize * lotSize;
            if (result < quantity) { result += lotSize; }

            return result;
        }
    }
}
