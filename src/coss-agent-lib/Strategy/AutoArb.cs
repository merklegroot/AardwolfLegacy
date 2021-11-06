using System.Collections.Generic;
using System.Linq;
using trade_browser_lib.Models;
using trade_model;

namespace trade_browser_lib.Strategy
{
    public class AutoArb
    {
        // Assumptions:
        // * The destination has enough volume that we can ignore the difference in its bids.
        // * Trading fees on both sides are negligable enough that with a high enough profit percentage, they can be ignored.        
        public List<OrderToPlace> Execute(
            List<Order> sourceEthAsks,
            List<Order> sourceBtcAsks,
            decimal destinationEthPrice,
            decimal destinationBtcPrice,
            decimal withdrawalFee,
            decimal ethBtcRatio)
        {
            const decimal PercentageThreshold = 1.0m;

            var goodEthOrders = new List<Order>();
            decimal profitFromEthOrders = 0;
            foreach (var ask in sourceEthAsks)
            {
                var priceDifference = destinationEthPrice - ask.Price;
                var percentDifference = 100.0m * priceDifference / ask.Price;
                if (percentDifference >= PercentageThreshold)
                {
                    goodEthOrders.Add(ask);
                    profitFromEthOrders += ask.Quantity * (destinationEthPrice - ask.Price);
                }
            }

            var ethSpent = goodEthOrders.Sum(item => item.Quantity * item.Price);
            var ethToReceive = goodEthOrders.Sum(item => item.Quantity * (destinationEthPrice - item.Price));

            var goodBtcOrders = new List<Order>();
            decimal profitFromBtcOrders = 0;
            foreach (var ask in sourceBtcAsks)
            {
                var priceDifference = destinationBtcPrice - ask.Price;
                var percentDifference = 100.0m * priceDifference / ask.Price;
                if (percentDifference >= PercentageThreshold)
                {
                    goodBtcOrders.Add(ask);
                    profitFromBtcOrders += ask.Quantity * (destinationEthPrice - ask.Price);
                }
            }

            var btcSpent = goodBtcOrders.Sum(item => item.Price * item.Quantity);
            var btcToReceive = goodBtcOrders.Sum(item => item.Quantity * (destinationBtcPrice - item.Price));

            var profitBeforeFees = profitFromEthOrders + profitFromBtcOrders;


            var profitAfterFees = profitBeforeFees - withdrawalFee;

            return null;
        }
    }
}
