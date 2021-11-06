using binance_lib;
using bit_z_lib;
using cache_lib.Models;
using console_lib;
using log_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using trade_model;

namespace bitz_agent_lib.App
{
    public class BitzAgentDriver : IBitzAgentDriver
    {
        private enum AskOrBid { Ask, Bid };

        public CachePolicy? OverrideCachePolicy { get; set; }

        private readonly IBitzIntegration _bitz;
        private readonly IBinanceIntegration _binance;
        private readonly ILogRepo _log;

        public BitzAgentDriver(
            IBitzIntegration bitzIntegration,
            IBinanceIntegration binanceIntegration,
            ILogRepo log)
        {
            _bitz = bitzIntegration;
            _binance = binanceIntegration;
            _log = log;
        }

        public void AutoOpenOrder()
        {
            AutoOpenOrder(new List<string>());
        }

        public void AutoOpenOrder(List<string> limitCommodities)
        {
            var cachePolicy = OverrideCachePolicy.HasValue ? OverrideCachePolicy.Value : CachePolicy.ForceRefresh;

            List<CommodityForExchange> bitzCommodities = null;
            List<TradingPair> bitzTradingPairs = null;
            List<CommodityForExchange> binanceCommodities = null;
            List<TradingPair> binanceTradingPairs = null;
            HoldingInfo bitzHoldings = null;

            var bitzTask = new Task(() =>
            {
                try
                {
                    bitzCommodities = _bitz.GetCommodities(cachePolicy);
                    bitzTradingPairs = _bitz.GetTradingPairs(cachePolicy);
                    bitzHoldings = _bitz.GetHoldings(cachePolicy);
                }
                catch(Exception exception)
                {
                    Error(exception);
                    throw;
                }
            }, TaskCreationOptions.LongRunning);

            bitzTask.Start();

            var binanceTask = new Task(() =>
            {
                try
                {
                    binanceCommodities = _binance.GetCommodities(cachePolicy);
                    binanceTradingPairs = _binance.GetTradingPairs(cachePolicy);
                }
                catch (Exception exception)
                {
                    Error(exception);
                    throw;
                }
            }, TaskCreationOptions.LongRunning);

            binanceTask.Start();

            Info("Loading Bit-Z and Binance commodity data.");

            bitzTask.Wait();
            binanceTask.Wait();

            var intersectingCommodities = GetBitzBinanceIntersections(
                bitzCommodities,
                binanceCommodities,
                binanceTradingPairs,
                bitzTradingPairs);

            Info($"Performing auto order on the following commodities: {string.Join(", ", intersectingCommodities.Select(item => item.Symbol))}");

            if (limitCommodities != null && limitCommodities.Any())
            {
                intersectingCommodities = intersectingCommodities.Where(item =>
                {
                    return limitCommodities.Any(queryLimitCommodity => string.Equals(item.Symbol, queryLimitCommodity, StringComparison.InvariantCultureIgnoreCase));                    
                }).ToList();
            }

            for (var i = 0; i < intersectingCommodities.Count; i++)
            {
                try
                {
                    var commodity = intersectingCommodities[i];

                    ConsoleWrapper.WriteLine($"Commodity: {commodity.Symbol} ({i + 1} of {intersectingCommodities.Count})");

                    AutoOpen(commodity,
                        bitzCommodities,
                        bitzTradingPairs,
                        binanceTradingPairs,
                        bitzHoldings);
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    ConsoleWrapper.WriteLine(exception);
                }
            }
        }

        private void Info(string message)
        {
            var currentTime = DateTime.UtcNow;
            ConsoleWrapper.WriteLine($"{currentTime} - {message}");
            _log.Info(message);
        }
        
        private void Error(Exception exception)
        {
            var currentTime = DateTime.UtcNow;
            ConsoleWrapper.WriteLine($"{currentTime} - Exception: {exception}");
            _log.Error(exception);
        }

        private void Error(string errorText)
        {
            var currentTime = DateTime.UtcNow;
            ConsoleWrapper.WriteLine($"{currentTime} - Exception: {errorText}");
            _log.Error(errorText);
        }

        private void AutoOpen(CommodityForExchange commodity,
           List<CommodityForExchange> bitzCommodities,
           List<TradingPair> bitzTradingPairs,
           List<TradingPair> binanceTradingPairs,
           HoldingInfo bitzHoldings)
        {
            var cachePolicy = OverrideCachePolicy.HasValue ? OverrideCachePolicy.Value : CachePolicy.ForceRefresh;

            var tradingPairsForSymbol = 
                bitzTradingPairs.Where(item => string.Equals(item.Symbol, commodity.Symbol, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            if (!tradingPairsForSymbol.Any()) { return; }
            
            var orderTypes = new List<AskOrBid> { AskOrBid.Ask, AskOrBid.Bid };

            foreach (var tradingPair in tradingPairsForSymbol)
            {
                _bitz.CancelAllOpenOrdersForTradingPair(tradingPair);

                var holdingsForSymbol = bitzHoldings.GetHoldingForSymbol(tradingPair.Symbol);
                if (holdingsForSymbol != null)
                {
                    if (holdingsForSymbol.InOrders > 0)
                    {
                        _bitz.CancelAllOpenOrdersForTradingPair(tradingPair);

                        holdingsForSymbol.Available += holdingsForSymbol.InOrders;
                        holdingsForSymbol.InOrders = 0;
                    }
                }

                foreach (var orderType in orderTypes)
                {
                    OpenOrder(commodity, tradingPair, cachePolicy, bitzHoldings, orderType);
                }
            }
        }

        private void OpenOrder(
            CommodityForExchange commodity,
            TradingPair tradingPair,
            CachePolicy cachePolicy,
            HoldingInfo bitzHoldings,
            AskOrBid orderType)
        {
            var holdingForSymbol = bitzHoldings.GetHoldingForSymbol(commodity.Symbol);      

            if (orderType == AskOrBid.Ask)
            {                
                if (holdingForSymbol == null || holdingForSymbol.Total <= 0)
                {
                    Info($"We don't have any {commodity.Symbol} to sell.");
                    return;
                }

                var holdingText = $"We have {holdingForSymbol.Total} {commodity.Symbol}.";
                if (holdingForSymbol.InOrders > 0)
                {
                    holdingText += $" {holdingForSymbol.InOrders} in orders and {holdingForSymbol.Available} available.";
                }
                Info(holdingText);

                if (commodity.MinimumTradeQuantity.HasValue && holdingForSymbol.Total < commodity.MinimumTradeQuantity.Value)
                {
                    Info(@"Not performing an open ask on {symbol}. The minimum trade quantity for {symbol} {matchingCommodity.MinimumTradeQuantity} is but we only have {holding.Total}.");
                    return;
                }
            }

            var bitzOrderBook = _bitz.GetOrderBook(tradingPair, cachePolicy);
            if (bitzOrderBook == null || bitzOrderBook.Asks == null || !bitzOrderBook.Asks.Any() || bitzOrderBook.Bids == null || !bitzOrderBook.Bids.Any())
            { return; }

            // no need to force binance to refresh every single time.
            // the normal cache expiry should be sufficient.
            var binanceOrderBook = _binance.GetOrderBook(tradingPair, cachePolicy != CachePolicy.ForceRefresh ? cachePolicy : CachePolicy.AllowCache);
            if (binanceOrderBook == null || binanceOrderBook.Bids == null || !binanceOrderBook.Bids.Any() || binanceOrderBook.Asks == null || !binanceOrderBook.Asks.Any())
            { return; }

            var lowestBinanceBid = binanceOrderBook.BestBid();
            if (lowestBinanceBid == null) { return; }

            var highestBinanceBidPrice = lowestBinanceBid.Price;
            if (highestBinanceBidPrice <= 0) { return; }

            var bestBinanceAsk = binanceOrderBook.BestAsk();
            if (bestBinanceAsk == null) { return; }

            var lowestBinanceAskPrice = bestBinanceAsk.Price;
            if (lowestBinanceAskPrice <= 0) { return; }

            var bestBitzAsk = bitzOrderBook.BestAsk();
            if (bestBitzAsk == null) { return; }

            var bestBitzAskPrice = bestBitzAsk.Price;
            if (bestBitzAskPrice <= 0) { return; }

            var bestBitzBid = bitzOrderBook.BestBid();
            var highestBitzBidPrice = bestBitzBid.Price;

            if (highestBitzBidPrice <= 0) { return; }

            if (orderType == AskOrBid.Ask)
            {
                var bidsWorthTaking =
                    bitzOrderBook.Bids.Where(queryBid => queryBid.Price >= lowestBinanceAskPrice)
                    .OrderByDescending(queryBid => queryBid.Price)
                    .ToList();

                if (bidsWorthTaking.Any())
                {
                    var goodBidQuantity = bidsWorthTaking.Sum(queryBid => queryBid.Quantity);
                    var worstPrice = bidsWorthTaking.Min(queryBid => queryBid.Price);
                    var effectiveBidQuantity = goodBidQuantity < holdingForSymbol.Available ? goodBidQuantity : holdingForSymbol.Total;

                    Info($"Selling {effectiveBidQuantity} {tradingPair.Symbol} outright at {worstPrice} {tradingPair.BaseSymbol}.");
                    try
                    {
                        _bitz.SellLimit(tradingPair, effectiveBidQuantity, worstPrice);
                    }
                    catch (Exception exception)
                    {
                        Error(exception);
                    }

                    // making some questionable assumptions here, but it saves us from having to reload the
                    // data and get closer to our rate limit.
                    holdingForSymbol.Available -= effectiveBidQuantity;
                    holdingForSymbol.Total -= effectiveBidQuantity;
                }

                if (holdingForSymbol.Available > 0)
                {
                    var desiredAskPrice = lowestBinanceAskPrice * 1.10m;

                    if (desiredAskPrice > bestBitzAskPrice) { desiredAskPrice = lowestBinanceAskPrice * 1.09m; }
                    if (desiredAskPrice > bestBitzAskPrice) { desiredAskPrice = lowestBinanceAskPrice * 1.08m; }
                    if (desiredAskPrice > bestBitzAskPrice) { desiredAskPrice = lowestBinanceAskPrice * 1.07m; }
                    if (desiredAskPrice > bestBitzAskPrice) { desiredAskPrice = lowestBinanceAskPrice * 1.06m; }
                    if (desiredAskPrice > bestBitzAskPrice) { desiredAskPrice = lowestBinanceAskPrice * 1.05m; }
                    if (desiredAskPrice > bestBitzAskPrice) { desiredAskPrice = lowestBinanceAskPrice * 1.04m; }
                    if (desiredAskPrice > bestBitzAskPrice) { desiredAskPrice = lowestBinanceAskPrice * 1.035m; }

                    if (desiredAskPrice > bestBitzAskPrice)
                    {
                        Info($"Bitz has no room for an ask on \"{tradingPair}\".");
                        return;
                    }

                    Info($"Aww yeah! Bitz has room for an ask on \"{tradingPair}\".");
                    Info($"Creating an ask for {holdingForSymbol.Available} {tradingPair.Symbol} at {desiredAskPrice} {tradingPair.BaseSymbol}");

                    _bitz.SellLimit(tradingPair, holdingForSymbol.Available, desiredAskPrice);
                    return;
                }
            }

            if (orderType == AskOrBid.Bid)
            {
                if (highestBitzBidPrice >= highestBinanceBidPrice)
                {
                    Info($"Bitz's best bid price of {highestBitzBidPrice} is already >= to binance's best bid {highestBinanceBidPrice} price on \"{tradingPair}\".");
                    return;
                }

                var targetBidPrice = highestBinanceBidPrice * 0.90m;
                if (targetBidPrice < highestBitzBidPrice) { targetBidPrice = highestBinanceBidPrice * 0.91m; }
                if (targetBidPrice < highestBitzBidPrice) { targetBidPrice = highestBinanceBidPrice * 0.92m; }
                if (targetBidPrice < highestBitzBidPrice) { targetBidPrice = highestBinanceBidPrice * 0.93m; }
                if (targetBidPrice < highestBitzBidPrice) { targetBidPrice = highestBinanceBidPrice * 0.94m; }
                if (targetBidPrice < highestBitzBidPrice) { targetBidPrice = highestBinanceBidPrice * 0.95m; }
                if (targetBidPrice < highestBitzBidPrice) { targetBidPrice = highestBinanceBidPrice * 0.96m; }
                if (targetBidPrice < highestBitzBidPrice) { targetBidPrice = highestBinanceBidPrice * 0.965m; }
                if (targetBidPrice < highestBitzBidPrice)
                {
                    Info($"There's no room for a bid on {tradingPair}");
                    return;
                }

                const decimal TargetEthQuantity = 0.1m;
                const decimal TargetBtcQuantity = 0.01m;

                decimal targetBaseQuantity;
                
                if (string.Equals(tradingPair.BaseSymbol, "ETH", StringComparison.InvariantCultureIgnoreCase))
                {
                    targetBaseQuantity = TargetEthQuantity;
                }
                else if (string.Equals(tradingPair.BaseSymbol, "BTC", StringComparison.InvariantCultureIgnoreCase))
                {
                    targetBaseQuantity = TargetBtcQuantity;
                }
                else
                {
                    Info($"This algorithm isn't equiped to work with {tradingPair.BaseSymbol} as a base commodity.");
                    return;
                }

                var targetSymbolQuantity = targetBaseQuantity / targetBidPrice;

                decimal availableBaseQuantity = bitzHoldings.GetHoldingForSymbol(tradingPair.BaseSymbol)?.Available ?? 0;
                if (availableBaseQuantity < targetBaseQuantity)
                {
                    Info($"We need {targetBaseQuantity} {tradingPair.BaseSymbol} to place an order, but we only have {availableBaseQuantity} {tradingPair.BaseSymbol} available.");
                    return;
                }

                Info($"About to place a bid on {_bitz.Name} for {targetSymbolQuantity} {tradingPair.Symbol} at {targetBidPrice} {targetSymbolQuantity}");

                var result = _bitz.BuyLimit(tradingPair, targetSymbolQuantity, targetBidPrice);
                if (result)
                {
                    Info($"Successfully placed a bid on {_bitz.Name} for {targetSymbolQuantity} {tradingPair.Symbol} at {targetBidPrice} {targetSymbolQuantity}");
                    var holdingsForBase = bitzHoldings.GetHoldingForSymbol(tradingPair.BaseSymbol);
                    if (holdingsForBase != null)
                    {
                        holdingsForBase.InOrders += targetSymbolQuantity * targetBidPrice;
                        holdingsForBase.Available -= targetSymbolQuantity * targetBidPrice;
                    }
                }
                else
                {
                    Error($"Failed to place a bid on {_bitz.Name} for {targetSymbolQuantity} {tradingPair.Symbol} at {targetBidPrice} {targetSymbolQuantity}");
                }

                return;
            }
        }

        private List<CommodityForExchange> GetBitzBinanceIntersections(
            List<CommodityForExchange> bitzCommodities,
            List<CommodityForExchange> binanceCommodities,
            List<TradingPair> binanceTradingPairs,
            List<TradingPair> bitzTradingPairs)
        {
            return bitzCommodities
                .Where(queryBitzCommodity =>
                {
                    // looking for trading commodities, not base commodities
                    var baseCommodities = new List<string> { "ETH", "BTC", "USDT" };
                    if (baseCommodities.Any(queryBaseCommodity => string.Equals(queryBitzCommodity.Symbol, queryBaseCommodity, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        return false;
                    }

                    if (!queryBitzCommodity.CanWithdraw.HasValue || !queryBitzCommodity.CanWithdraw.Value)
                    {
                        return false;
                    }

                    if (!queryBitzCommodity.CanDeposit.HasValue || !queryBitzCommodity.CanDeposit.Value)
                    {
                        return false;
                    }

                    var matchingBinanceCommodity = binanceCommodities.SingleOrDefault(item =>
                        string.Equals(item.Symbol, queryBitzCommodity.Symbol, StringComparison.InvariantCultureIgnoreCase));

                    if (matchingBinanceCommodity == null || 
                        !matchingBinanceCommodity.CanDeposit.HasValue || !matchingBinanceCommodity.CanDeposit.Value
                        || !matchingBinanceCommodity.CanWithdraw.HasValue || !matchingBinanceCommodity.CanWithdraw.Value)
                    {
                        return false;
                    }

                    if (!bitzTradingPairs.Any(queryBitzTradingPair =>
                        string.Equals(queryBitzTradingPair.Symbol, queryBitzCommodity.Symbol, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        return false;
                    }

                    if (!binanceTradingPairs.Any(queryBinanceTradingPair =>
                        string.Equals(queryBinanceTradingPair.Symbol, queryBitzCommodity.Symbol, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        return false;
                    }

                    return true;
                })
                .Distinct()
                .ToList();
        }        
    }
}
