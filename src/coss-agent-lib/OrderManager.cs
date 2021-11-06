using trade_lib;
using log_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using trade_browser_lib.Models;
using trade_model;

namespace trade_browser_lib
{
    public class OrderManager : IOrderManager
    {
        private const bool IsInstantProfitEnabled = false;

        private const decimal Increment = 0.00000005m;
        private const decimal SellingProfitPercentageThreshold = 0.25m;
        private const decimal BuyingProfitPercentageThreshold = 0.75m;

        private const decimal BuyingEthBtcProfitPercentageThreshold = 0.025m;

        private readonly ILogRepo _log;

        public OrderManager(ILogRepo log)
        {
            _log = log;
        }
        
        public void ManageOrders(
            TradingPair tradingPair,
            List<OpenOrderEx> myOpenOrders,
            OrderBook cossOrderBook,
            OrderBook binanceOrderBook,
            Action<OpenOrder> placeBid,
            Action<OpenOrder> placeAsk)
        {
            var bestCossAsk = cossOrderBook.BestAsk();
            var bestBinanceBid = binanceOrderBook.BestBid();

            if (IsInstantProfitEnabled && bestCossAsk != null && bestBinanceBid != null && bestCossAsk.Price > 0 && bestBinanceBid.Price > 0)
            {
                var immediateProfitPercentage =
                    100.0m * (bestBinanceBid.Price - bestCossAsk.Price) / bestCossAsk.Price;

                const decimal MinimumImmediateQuantity = 0.0001m;
                const decimal MinimumImmediateProfitPercentage = 0.25m;

                if (immediateProfitPercentage > MinimumImmediateProfitPercentage &&
                    bestCossAsk.Quantity > MinimumImmediateQuantity)
                {
                    placeBid(new OpenOrder { Price = bestCossAsk.Price, Quantity = bestCossAsk.Quantity });

                    // if there are more good offers, we'll get them the next round.
                    return;
                }
            }

            for (var i = myOpenOrders.Count() - 1; i >= 0; i--)
            {
                HandleOrder(
                    myOpenOrders[i],
                    tradingPair,
                    myOpenOrders,
                    cossOrderBook,
                    binanceOrderBook,
                    placeBid,
                    placeAsk);
            }
        }

        private void HandleOrder(
            OpenOrderEx order,
            TradingPair tradingPair,
            List<OpenOrderEx> myOpenOrders,
            OrderBook cossOrderBook,
            OrderBook binanceOrderBook,
            Action<OpenOrder> placeBid,
            Action<OpenOrder> placeAsk)
        {
            var binanceLowestAsk = binanceOrderBook.BestAsk();
            var binanceHighestBid = binanceOrderBook.BestBid();

            var isBidTooHigh = order.OrderType == 
                OrderType.Bid && order.Price > binanceHighestBid.Price;

            var isAskTooLow = order.OrderType == 
                OrderType.Ask && order.Price < binanceLowestAsk.Price;

            var binanceText = order.OrderType == OrderType.Bid ? $"Binance bid: {binanceHighestBid}" : $"Binance ask: {binanceLowestAsk}";

            if (isBidTooHigh || isAskTooLow)
            {
                var aboveOrBelow = order.OrderType == OrderType.Bid ? "above" : "below";
                _log.Info(new StringBuilder()
                    .AppendLine($"Cancelling your {order.OrderType} order for {order.Quantity} {tradingPair.Symbol} at {order.Price} {tradingPair.BaseSymbol} because it is no longer profitable.")
                    .AppendLine(binanceText)
                    .ToString());

                order.Cancel();

                return;
            }

            if (order.OrderType == OrderType.Bid)
            {
                var highestCossBid = cossOrderBook.Bids.OrderByDescending(item => item.Price).FirstOrDefault();
                if (highestCossBid != null && highestCossBid.Price > order.Price)
                {
                    var betterBid = highestCossBid.Price + Increment;

                    order.Cancel();

                    var profitPercentange = 100.0m * ((binanceHighestBid.Price - order.Price) / order.Price);
                    
                    if (profitPercentange >= BuyingProfitPercentageThreshold || 
                        (tradingPair.Equals(new TradingPair("ETH", "BTC")) && profitPercentange >= BuyingEthBtcProfitPercentageThreshold))
                    {
                        _log.Info(new StringBuilder()
                            .AppendLine($"Your order for {order.Quantity} {tradingPair.Symbol} at {order.Price} {tradingPair.BaseSymbol} has been outbid.")
                            .AppendLine($"Cancelling this order placing a new order at {betterBid}.")
                            .AppendLine($"That puts the profit margin at {profitPercentange}%.")
                            .ToString(),
                            TradeEventType.RaiseBid);

                        placeBid(new OpenOrder { Quantity = order.Quantity, Price = betterBid });
                    }
                    else
                    {
                        _log.Info(new StringBuilder()
                            .AppendLine($"Your order for {order.Quantity} {tradingPair.Symbol} at {order.Price} {tradingPair.BaseSymbol} has been outbid.")
                            .AppendLine($"A higher bid would limit the profit margin to {profitPercentange}% which would put us below the {BuyingProfitPercentageThreshold}% threshold.")
                            .AppendLine($"Cancelling this order and moving on.")
                            .ToString(),
                            TradeEventType.CancelOrder);
                    }

                    return;
                }
            }
            else if (order.OrderType == OrderType.Ask)
            {
                var lowestCossAsk = cossOrderBook.Asks.OrderBy(item => item.Price).FirstOrDefault();
                if (lowestCossAsk != null && lowestCossAsk.Price < order.Price)
                {
                    var betterAsk = lowestCossAsk.Price - Increment;

                    order.Cancel();

                    var profitPercentange = 100.0m * ((order.Price - binanceHighestBid.Price) / order.Price);
                    const decimal ProfitPercentageThreshold = 1.0m;
                    if (profitPercentange >= ProfitPercentageThreshold)
                    {
                        _log.Info(new StringBuilder()
                            .AppendLine($"Your order for {order.Quantity} {tradingPair.Symbol} at {order.Price} {tradingPair.BaseSymbol} has been outasked.")
                            .AppendLine($"Cancelling this order placing a new order at {betterAsk}.")
                            .AppendLine($"That puts the profit margin at {profitPercentange}%.")
                            .ToString(),
                            TradeEventType.LowerAsk);

                        placeAsk(new OpenOrder { Quantity = order.Quantity, Price = betterAsk });
                    }
                    else
                    {
                        _log.Info(new StringBuilder()
                            .AppendLine($"Your order for {order.Quantity} {tradingPair.Symbol} at {order.Price} {tradingPair.BaseSymbol} has been outasked.")
                            .AppendLine($"A lower ask would limit the profit margin to {profitPercentange}% which would put below the {ProfitPercentageThreshold}% threshold.")
                            .AppendLine($"Cancelling this order and moving on.")
                            .ToString(),
                            TradeEventType.CancelOrder);
                    }

                    return;
                }
            }

            _log.Info($"Keeping Coss {order.OrderType} Order for {order.Quantity} {tradingPair.Symbol} @{order.Price} {tradingPair.BaseSymbol} is still profitable. {binanceText}", TradeEventType.KeepOrder);
        }
    }
}
