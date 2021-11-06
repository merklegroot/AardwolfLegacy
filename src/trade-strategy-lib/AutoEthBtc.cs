using System;
using System.Collections.Generic;
using System.Linq;
using trade_model;

namespace trade_strategy_lib
{
    public class AutoEthBtc
    {
        private const decimal RoundingCorrection = 0.00001m;

        public StrategyAction Execute(
            OrderBook lowVolumeOrderBook,
            OrderBook highVolumeOrderBook,
            decimal minimumProfitPercentage,
            decimal minimumEthVolume,
            decimal? minimumBtcVolume = null)
        {
            if (lowVolumeOrderBook == null) { throw new ArgumentNullException(nameof(lowVolumeOrderBook)); }
            if (highVolumeOrderBook == null) { throw new ArgumentNullException(nameof(highVolumeOrderBook)); }

            // assuming that BTC <= 15 * ETH.
            // right now, it's about 8 * ETH, so 15 is a wide buffer.
            // however, this may oneday change.
            var effectiveMinimumBtcVolume = minimumBtcVolume ?? minimumEthVolume * 15.0m;
           
            var ordersToBuy = new List<Order>();
            if (highVolumeOrderBook?.Bids?.Any() ?? false)
            {
                var bestHighVolumeBid = highVolumeOrderBook.BestBid();
                foreach (var ask in lowVolumeOrderBook.Asks ?? new List<Order>())
                {
                    if (ask == null || ask.Quantity <= 0 || ask.Price <= 0) { continue; }

                    var profit = bestHighVolumeBid.Price - ask.Price;
                    var profitPercentage = 100.0m * profit / ask.Price;
                    if (profitPercentage >= minimumProfitPercentage)
                    {
                        ordersToBuy.Add(ask);
                    }
                }
            }

            var ordersToSell = new List<Order>();
            if (highVolumeOrderBook?.Asks?.Any() ?? false)
            {
                var bestHighVolumeAsk = highVolumeOrderBook.BestAsk();
                foreach (var bid in lowVolumeOrderBook.Bids ?? new List<Order>())
                {
                    if (bid == null || bid.Quantity <= 0 || bid.Price <= 0) { continue; }

                    var profit = bid.Price - bestHighVolumeAsk.Price;
                    var profitPercentage = 100.0m * profit / bid.Price;
                    if (profitPercentage >= minimumProfitPercentage)
                    {
                        ordersToSell.Add(bid);
                    }
                }
            }

            // Sometimes we can find orders that we want to both buy and sell.
            // This can happen when there's an order in the system with a quantity
            // so low that the system disallows completing the trade.
            if (ordersToSell.Any() && ordersToBuy.Any())
            {
                var sellQuantity = ordersToSell.Sum(item => item.Quantity);
                var buyQuantity = ordersToBuy.Sum(item => item.Quantity);

                // this indicates that something weird is going on.
                // just back out.
                if (sellQuantity == buyQuantity)
                {
                    return new StrategyAction { ActionType = StrategyActionEnum.DoNothing };
                }
                else if(sellQuantity > buyQuantity)
                {
                    ordersToBuy.Clear();
                }
                else
                {
                    ordersToSell.Clear();
                }
            }

            if (ordersToSell.Any())
            {
                var quantity = ordersToSell.Sum(order => order.Quantity) + RoundingCorrection;
                if (quantity < effectiveMinimumBtcVolume) { quantity = effectiveMinimumBtcVolume + RoundingCorrection; }

                var price = ordersToSell.Min(order => order.Price);

                return new StrategyAction
                {
                    ActionType = StrategyActionEnum.PlaceAsk,
                    Price = price,
                    Quantity = quantity
                };
            }

            if (ordersToBuy.Any())
            {
                var quantity = ordersToBuy.Sum(order => order.Quantity) + RoundingCorrection;
                if (quantity < minimumEthVolume) { quantity = minimumEthVolume + RoundingCorrection; }

                var price = ordersToBuy.Max(order => order.Price);

                return new StrategyAction
                {
                    ActionType = StrategyActionEnum.PlaceBid,
                    Price = price,
                    Quantity = quantity
                };
            }

            return new StrategyAction { ActionType = StrategyActionEnum.DoNothing };
        }
    }
}
