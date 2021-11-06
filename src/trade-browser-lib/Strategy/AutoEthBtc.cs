using System;
using System.Collections.Generic;
using System.Linq;
using trade_browser_lib.Models;
using trade_model;

namespace trade_browser_lib
{
    public class AutoEthBtc
    {
        private const decimal ProfitPercentageThreshold = 1.0m;
       
        public List<OrderToPlace> Execute(OrderBook cossOrderBook, OrderBook binanceOrderBook)
        {
            var ordersToPlace = new List<OrderToPlace>();

            var bestBinanceAsk = binanceOrderBook.BestAsk();
            var bestBinanceBid = binanceOrderBook.BestBid();

            var shouldBuy = new Func<Order, bool>(cossAsk =>
            {
                if (bestBinanceBid == null) { return false; }
                var diff = bestBinanceBid.Price - cossAsk.Price;

                var percentDifference = 100.0m * (diff / bestBinanceBid.Price);
                return percentDifference >= ProfitPercentageThreshold;
            });

            var ordersWorthBuying = cossOrderBook.Asks?.Where(order => shouldBuy(order)).ToList() ?? new List<Order>();
            if (ordersWorthBuying.Any())
            {
                var worstOrderThatWeAreWillingToBuy = ordersWorthBuying.OrderByDescending(order => order.Price).First();
                var quantity = ordersWorthBuying.Select(item => item.Quantity).Sum() + 0.00001m;

                var orderToPlace = new OrderToPlace { OrderType = OrderType.Bid, Price = worstOrderThatWeAreWillingToBuy.Price, Quantity = quantity };

                ordersToPlace.Add(orderToPlace);
            }

            var shouldSell = new Func<Order, bool>(cossOrder =>
            {
                var diff = cossOrder.Price - bestBinanceAsk.Price;
                var percentDifference = 100.0m * (diff / bestBinanceAsk.Price);
                return percentDifference > 1.0m;
            });

            var ordersWorthSelling = cossOrderBook.Bids?.Where(order => shouldSell(order)).ToList() ?? new List<Order>();
            if (ordersWorthSelling.Any())
            {
                var worstOrderThatWeAreWillingToSell = ordersWorthSelling.OrderBy(order => order.Price).First();
                var quantity = ordersWorthSelling.Select(item => item.Quantity).Sum() + 0.00001m;

                var orderToPlace = new OrderToPlace { OrderType = OrderType.Ask, Price = worstOrderThatWeAreWillingToSell.Price, Quantity = quantity };

                ordersToPlace.Add(orderToPlace);
            }

            return ordersToPlace;
        }
    }
}
