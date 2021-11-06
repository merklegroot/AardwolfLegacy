using cache_lib.Models;
using math_lib;
using object_extensions_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using task_lib;
using trade_constants;
using trade_model;

namespace coss_arb_lib
{
    public partial class CossArbUtil
    {
        private static Dictionary<string, decimal> MaxAcquireDictionary = new Dictionary<string, decimal>
        {
            { "NEO", 25.0m }
        };

        public void AcquireAgainstBinanceSymbolV5(string sym)
        {
            var symbol = sym.Trim().ToUpper();

            // const decimal OptimalEthQuantityToSpend = 0.5m;
            // const decimal NonOptimalEthQuantityToSpend = 0.25m;
            const decimal OptimalEthQuantityToSpend = 0.35m;
            const decimal NonOptimalEthQuantityToSpend = 0.15m;

            // const decimal OptimalBtcQuantityToSpend = 0.02m;
            // const decimal NonOptimalBtcQuantityToSpend = 0.01m;
            const decimal OptimalBtcQuantityToSpend = 0.015m;
            const decimal NonOptimalBtcQuantityToSpend = 0.0075m;

            const decimal OptimalDollarCoinQuantityToSpend = 200.0m;
            const decimal NonOptimalDollarCoinQuantityToSpend = 100.0m;

            const decimal DollarCoinMinInstantSellProfitPercent = 1.75m;

            const decimal MinimumCossPar = 250.0m;

            var standardBaseSymbols = new List<string> { "ETH", "BTC" };

            // effective way to avoid fiat until we have KYC.
            var potentialBaseSymbols = new List<string> { "ETH", "BTC", "TUSD", "USDT", "COSS",
                "XRP"
            };

            var optimalQuantityToSpendDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "ETH", OptimalEthQuantityToSpend },
                { "BTC", OptimalBtcQuantityToSpend }
            };

            var nonOptimalQuantityToSpendDictioanry = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "ETH", NonOptimalEthQuantityToSpend },
                { "BTC", NonOptimalBtcQuantityToSpend }
            };

            var minimumTransactionQuantityDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "ETH", MinimumTradeEth },
                { "BTC", MinimumTradeBtc },
                { "COSS", MinimumTradeCoss },
                { "TUSD", MinimumTradeTusd },
                { "USDT", MinimumTradeUsdt },
                { "XRP", MinimumTradeXrp }
            };

            const int MaxPriceDecimals = 8;
            const decimal OptimalQuantityToBuy = 5.0m;

            List<TradingPair> binanceTradingPairs = null;
            DetailedExchangeCommodity binanceCommodity = null;
            OrderBook binanceEthBtcOrderBook = null;
            OrderBook binanceSymbolEthOrderBook = null;
            OrderBook binanceSymbolBtcOrderBook = null;
            Dictionary<string, OrderBook> binanceOrderBooks = new Dictionary<string, OrderBook>();

            decimal ethBtcRatio;

            var shouldCancelAllCossOrders = false;
            var binanceTask = LongRunningTask.Run(() =>
            {
                binanceTradingPairs = _exchangeClient.GetTradingPairs(IntegrationNameRes.Binance, CachePolicy.AllowCache);
                binanceCommodity = _exchangeClient.GetCommoditiyForExchange(IntegrationNameRes.Binance, symbol, null, CachePolicy.AllowCache);
                if (binanceTradingPairs == null || !binanceTradingPairs.Any())
                {
                    binanceTradingPairs = _exchangeClient.GetTradingPairs(IntegrationNameRes.Binance, CachePolicy.ForceRefresh);
                    if (binanceTradingPairs == null || !binanceTradingPairs.Any())
                    {
                        shouldCancelAllCossOrders = true;
                        _log.Error("Failed to retrieve binance trading pairs.");
                    }
                }

                if (binanceTradingPairs != null && binanceTradingPairs.Any())
                {
                    binanceEthBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, "ETH", "BTC", CachePolicy.ForceRefresh);
                    binanceOrderBooks["ETH-BTC"] = binanceEthBtcOrderBook;

                    var binanceEthBtcBestAskPrice = binanceEthBtcOrderBook.BestAsk().Price;
                    if (binanceEthBtcBestAskPrice <= 0) { throw new ApplicationException("Binance's best ETH-BTC ask price should be > 0."); }

                    var binanceEthBtcBestBidPrice = binanceEthBtcOrderBook.BestBid().Price;
                    if (binanceEthBtcBestBidPrice <= 0) { throw new ApplicationException("Binance's best ETH-BTC bid price should be > 0."); }

                    ethBtcRatio = new List<decimal> { binanceEthBtcBestAskPrice, binanceEthBtcBestBidPrice }.Average();

                    if (binanceTradingPairs.Any(item =>
                        string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                        && string.Equals(item.BaseSymbol, "ETH", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        binanceSymbolEthOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, symbol, "ETH", CachePolicy.ForceRefresh);
                        binanceOrderBooks[$"{symbol}-ETH"] = binanceSymbolEthOrderBook;
                    }

                    if (binanceTradingPairs.Any(item =>
                        string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                        && string.Equals(item.BaseSymbol, "BTC", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        binanceSymbolBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, symbol, "BTC", CachePolicy.ForceRefresh);
                        binanceOrderBooks[$"{symbol}-BTC"] = binanceSymbolBtcOrderBook;
                    }

                    if (!binanceOrderBooks.ContainsKey($"{symbol}-ETH"))
                    {
                        var clonedBtcOrderBook = binanceOrderBooks[$"{symbol}-BTC"].CloneAs<OrderBook>();
                        var ethAsks = clonedBtcOrderBook.Asks.Select(queryOrder => new Order
                        {
                            Quantity = queryOrder.Quantity,
                            Price = queryOrder.Price / ethBtcRatio
                        }).ToList();

                        var ethBids = clonedBtcOrderBook.Bids.Select(queryOrder => new Order
                        {
                            Quantity = queryOrder.Quantity,
                            Price = queryOrder.Price / ethBtcRatio
                        }).ToList();

                        var equivalentEthOrderBook = new OrderBook
                        {
                            Asks = ethAsks,
                            Bids = ethBids
                        };

                        binanceOrderBooks[$"{symbol}-ETH"] = equivalentEthOrderBook;
                    }
                }
            });

            var cossTradingPairs = _exchangeClient.GetTradingPairs(IntegrationNameRes.Coss, CachePolicy.AllowCache);
            var cossCommodities = _exchangeClient.GetCommoditiesForExchange(IntegrationNameRes.Coss, CachePolicy.AllowCache);
            var cossCommodity = cossCommodities.SingleOrDefault(item => string.Equals(item.Symbol, symbol));

            var getTradingPair = new Func<string, string, TradingPair>((argSymbol, argBaseSymbol) =>
            {
                var matchingTradingPair = cossTradingPairs.SingleOrDefault(item => string.Equals(item.Symbol, argSymbol, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(item.BaseSymbol, argBaseSymbol));

                return matchingTradingPair;
            });

            var getLotSize = new Func<string, string, decimal>((argSymbol, argBaseSymbol) =>
            {
                var matchingTradingPair = getTradingPair(argSymbol, argBaseSymbol);

                if (matchingTradingPair != null && matchingTradingPair.LotSize.HasValue)
                {
                    return matchingTradingPair.LotSize.Value;
                }

                if (cossCommodity.LotSize.HasValue) { return cossCommodity.LotSize.Value; }

                return DefaultLotSize;
            });

            var getPriceTick = new Func<string, string, decimal>((argSymbol, arbBaseSymbol) =>
            {
                var matchingTradingPair = cossTradingPairs.SingleOrDefault(item => string.Equals(item.Symbol, argSymbol, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(item.BaseSymbol, arbBaseSymbol));

                if (matchingTradingPair != null && matchingTradingPair.PriceTick.HasValue) { return matchingTradingPair.PriceTick.Value; }

                return DefaultPriceTick;
            });

            var commodity = cossCommodities.Single(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase));

            var cossTradingPairsForSymbol = cossTradingPairs.Where(item =>
                string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                && potentialBaseSymbols.Contains(item.BaseSymbol)).ToList();

            var cossBaseSymbolsForSymbol = cossTradingPairsForSymbol.Select(item => item.BaseSymbol).Distinct().ToList();

            binanceTask.Wait();
            var areBinanceDepositsAndWithdrawalsOpen = binanceCommodity.CanDeposit.HasValue && binanceCommodity.CanDeposit.Value && binanceCommodity.CanWithdraw.HasValue && binanceCommodity.CanWithdraw.Value;
            var shouldWeAvoidPurchasing = cossCommodity == null
                || (cossCommodity.CanWithdraw.HasValue && !cossCommodity.CanWithdraw.Value);

            if (shouldCancelAllCossOrders)
            {
                foreach (var baseSymbol in cossBaseSymbolsForSymbol) { CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, symbol, baseSymbol); }
                return;
            }

            var remainingOpenOrders = new List<OpenOrderForTradingPair>();
            var baseSymbolsForKeptBids = new List<string>();
            foreach (var baseSymbol in standardBaseSymbols.Where(queryStandardBaseSymbol => cossBaseSymbolsForSymbol.Any(queryCossBaseSymbol => string.Equals(queryStandardBaseSymbol, queryCossBaseSymbol, StringComparison.InvariantCultureIgnoreCase))))
            {
                var openOrders = GetOpenOrdersWithRetries(symbol, baseSymbol, CachePolicy.ForceRefresh) ?? new List<OpenOrderForTradingPair>();
                var openAsks = openOrders.Where(item => item.OrderType == OrderType.Ask).ToList();
                openAsks.ForEach(openOrder => CancelOpenOrderWithRetries(IntegrationNameRes.Coss, openOrder.OrderId));

                var openBids = openOrders.Where(item => item.OrderType == OrderType.Bid).ToList();
                if (openBids.Count != 1 || !shouldWeAvoidPurchasing)
                {
                    if (openBids.Any())
                    {
                        openBids.ForEach(openOrder => CancelOpenOrderWithRetries(IntegrationNameRes.Coss, openOrder.OrderId));
                    }
                }
                else
                {
                    var shouldKeepBid = false;

                    var openBid = openBids.Single();
                    var cossSymbolBaseSymbolOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, symbol, baseSymbol, CachePolicy.ForceRefresh);
                    var clonedOrderBook = cossSymbolBaseSymbolOrderBook.CloneAs<OrderBook>();
                    var matchingBid = clonedOrderBook.Bids.Where(item => item.Price == openBid.Price && item.Quantity == openBid.Quantity).FirstOrDefault();
                    if (matchingBid != null)
                    {
                        clonedOrderBook.Bids.Remove(matchingBid);

                        if (standardBaseSymbols.Contains(baseSymbol))
                        {
                            var binanceSymbolBaseSymbolOrderBook = binanceOrderBooks[$"{symbol}-{baseSymbol}"];
                            var binanceSymbolBaseSymbolBestBidPrice = binanceSymbolBaseSymbolOrderBook.BestBid().Price;
                            if (binanceSymbolBaseSymbolBestBidPrice <= 0) { throw new ApplicationException($"Binance's best bid price for {symbol}-{baseSymbol} should be > 0."); }

                            var cossSymbolBaseSymbolBestBidPrice = clonedOrderBook.BestBid().Price;
                            if (cossSymbolBaseSymbolBestBidPrice <= 0) { throw new ApplicationException($"Coss's best bid price for {symbol}-{baseSymbol} should be > 0."); }

                            var optimalBidPrice = MathUtil.Truncate(binanceSymbolBaseSymbolBestBidPrice * (100.0m - OptimalPercentDiff) / 100.0m, MaxPriceDecimals);

                            if (optimalBidPrice >= cossSymbolBaseSymbolBestBidPrice)
                            {
                                if (optimalBidPrice == openBid.Price)
                                {
                                    shouldKeepBid = true;
                                }
                            }
                            else
                            {
                                var upTickBidPrice = cossSymbolBaseSymbolBestBidPrice + DefaultPriceTick;
                                var diff = binanceSymbolBaseSymbolBestBidPrice - upTickBidPrice;
                                var diffRatio = diff / binanceSymbolBaseSymbolBestBidPrice;
                                var percentDiff = 100.0m * diffRatio;

                                if (percentDiff >= MinOpenBidPercentDiff)
                                {
                                    if (openBid.Price == upTickBidPrice)
                                    {
                                        shouldKeepBid = true;
                                    }
                                }
                            }
                        }
                        else if (string.Equals(baseSymbol, "COSS", StringComparison.InvariantCultureIgnoreCase))
                        {

                        }
                    }
                    else
                    {
                        shouldKeepBid = false;
                    }

                    if (shouldKeepBid)
                    {
                        baseSymbolsForKeptBids.Add(baseSymbol);
                    }
                    else
                    {
                        CancelOpenOrderWithRetries(IntegrationNameRes.Coss, openBid.OrderId);
                    }
                }
            }

            var balances = GetCossBalancesWithRetries(CachePolicy.ForceRefresh);

            var availableBalances = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase);
            availableBalances[symbol] = balances.GetAvailableForSymbol(symbol);
            availableBalances["ETH"] = balances.GetAvailableForSymbol("ETH");
            availableBalances["BTC"] = balances.GetAvailableForSymbol("BTC");
            availableBalances["COSS"] = balances.GetAvailableForSymbol("COSS");
            availableBalances["TUSD"] = balances.GetAvailableForSymbol("TUSD");
            availableBalances["USDT"] = balances.GetAvailableForSymbol("USDT");
            availableBalances["XRP"] = balances.GetAvailableForSymbol("XRP");

            if (MaxAcquireDictionary.ContainsKey(symbol))
            {
                var maxAcquireValue = MaxAcquireDictionary[symbol];
                var totalSymbolBalance = balances.GetTotalForSymbol(symbol);
                if (totalSymbolBalance >= maxAcquireValue) { shouldWeAvoidPurchasing = true; }
            }

            var askPricesToPlaceDictionary = new Dictionary<string, decimal>();

            var availableStandardBaseSymbols = cossBaseSymbolsForSymbol.Where(item => standardBaseSymbols.Contains(item)).ToList();
            foreach (var baseSymbol in availableStandardBaseSymbols)
            {
                var binanceSymbolBaseSymbolOrderBook = binanceOrderBooks[$"{symbol}-{baseSymbol}"];

                var binanceSymbolBaseSymbolBestBidPrice = binanceSymbolBaseSymbolOrderBook.BestBid().Price;
                if (binanceSymbolBaseSymbolBestBidPrice <= 0) { throw new ApplicationException($"Binance {symbol}-{baseSymbol} best bid price should be > 0."); }

                var binanceSymbolBaseSymbolBestAskPrice = binanceSymbolBaseSymbolOrderBook.BestAsk().Price;
                if (binanceSymbolBaseSymbolBestAskPrice <= 0) { throw new ApplicationException($"Binance {symbol}-{baseSymbol} best ask price should be > 0."); }

                var cossSymbolBaseSymbolOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, symbol, baseSymbol, CachePolicy.ForceRefresh);
                var cossSymbolBaseSymbolBestBid = cossSymbolBaseSymbolOrderBook.BestBid();
                var cossSymbolBaseSymbolBestBidPrice = cossSymbolBaseSymbolBestBid.Price;
                if (cossSymbolBaseSymbolBestBidPrice <= 0) { throw new ApplicationException($"Coss {symbol}-{baseSymbol} best bid price should be > 0."); }

                var cossSymbolBaseSymbolBestAsk = cossSymbolBaseSymbolOrderBook.BestAsk();
                var cossSymbolBaseSymbolBestAskPrice = cossSymbolBaseSymbolBestAsk.Price;
                if (cossSymbolBaseSymbolBestAskPrice <= 0) { throw new ApplicationException($"Coss {symbol}-{baseSymbol} best ask price should be > 0."); }

                var instantAskDiff = cossSymbolBaseSymbolBestBidPrice - binanceSymbolBaseSymbolBestAskPrice;
                var instantAskDiffRatio = instantAskDiff / binanceSymbolBaseSymbolBestAskPrice;
                var instantAskPercentDiff = 100.0m * instantAskDiffRatio;

                if (instantAskPercentDiff >= MinInstantAskPercentDiff)
                {
                    var quantity = cossSymbolBaseSymbolBestBid.Quantity > availableBalances[symbol]
                        ? MathUtil.ConstrainToMultipleOf(availableBalances[symbol], getLotSize(symbol, baseSymbol))
                        : cossSymbolBaseSymbolBestBid.Quantity;

                    var price = cossSymbolBaseSymbolBestBidPrice;

                    var transactionQuantity = quantity * price;
                    var minimumTransactionQuantity = minimumTransactionQuantityDictionary[baseSymbol];
                    if (transactionQuantity < minimumTransactionQuantity * 1.01m)
                    {
                        quantity = MathUtil.ConstrainToMultipleOf(1.0m * minimumTransactionQuantity / price, getLotSize(symbol, baseSymbol));
                    }

                    if (quantity > 0)
                    {
                        _log.Info($"About to place an ask on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                        try
                        {
                            var orderResult = _exchangeClient.SellLimitV2(IntegrationNameRes.Coss, symbol, baseSymbol, new QuantityAndPrice
                            {
                                Quantity = quantity,
                                Price = price
                            });

                            if (orderResult.WasSuccessful)
                            {
                                availableBalances[symbol] -= quantity * 1.01m;
                                _log.Info($"Successfully placed an ask on Coss for {quantity} {symbol} at {price} {baseSymbol}.");

                                if (quantity >= cossSymbolBaseSymbolBestBid.Quantity)
                                {
                                    cossSymbolBaseSymbolOrderBook.Bids.Remove(cossSymbolBaseSymbolBestBid);
                                    cossSymbolBaseSymbolBestBid = cossSymbolBaseSymbolOrderBook.BestBid();
                                    cossSymbolBaseSymbolBestBid.Price = cossSymbolBaseSymbolBestBid.Price;
                                }
                                else
                                {
                                    cossSymbolBaseSymbolBestBid.Quantity -= quantity;
                                }
                            }
                            else
                            {
                                _log.Error($"Failed to place an ask on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                            }
                        }
                        catch (Exception exception)
                        {
                            _log.Error($"Failed to place an ask on Coss for {quantity} {symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                            _log.Error(exception);
                        }
                    }
                }

                var instantBidDiff = binanceSymbolBaseSymbolBestBidPrice - cossSymbolBaseSymbolBestAskPrice;
                var instantBidDiffRatio = instantBidDiff / binanceSymbolBaseSymbolBestBidPrice;
                var instantBidPercentDiff = 100.0m * instantBidDiffRatio;

                if (instantBidPercentDiff >= MinInstantBidPercentDiff)
                {
                    var shouldPlaceInstantBid = true;

                    var price = cossSymbolBaseSymbolBestAskPrice;

                    var potentialBaseSymbolAmountToSpend = cossSymbolBaseSymbolBestAskPrice * cossSymbolBaseSymbolBestAsk.Quantity * 1.01m;

                    var quantity = potentialBaseSymbolAmountToSpend > availableBalances[baseSymbol]
                        ? MathUtil.ConstrainToMultipleOf(availableBalances[baseSymbol] / 1.01m, getLotSize(symbol, baseSymbol))
                        : MathUtil.ConstrainToMultipleOf(cossSymbolBaseSymbolBestAsk.Quantity * 1.001m, getLotSize(symbol, baseSymbol));

                    if (MinimumTradeDictionary.ContainsKey(baseSymbol))
                    {
                        var minimumTrade = MinimumTradeDictionary[baseSymbol];
                        if (quantity * price < minimumTrade * 1.01m)
                        {
                            quantity = MathUtil.ConstrainToMultipleOf(minimumTrade / price, getLotSize(symbol, baseSymbol));

                            if (price * quantity * 1.01m > availableBalances[baseSymbol])
                            {
                                shouldPlaceInstantBid = false;
                            }
                        }
                    }

                    if (shouldPlaceInstantBid && !shouldWeAvoidPurchasing)
                    {
                        try
                        {
                            _log.Info($"About to place a bid on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                            var orderResult = _exchangeClient.BuyLimitV2(IntegrationNameRes.Coss, symbol, baseSymbol, new QuantityAndPrice
                            {
                                Quantity = quantity,
                                Price = price
                            });

                            if (orderResult.WasSuccessful)
                            {
                                availableBalances[baseSymbol] -= quantity * price * 1.01m;
                                if (availableBalances[baseSymbol] < 0) { availableBalances[baseSymbol] = 0; }

                                if (price == cossSymbolBaseSymbolBestAsk.Price && quantity >= cossSymbolBaseSymbolBestAsk.Quantity)
                                {
                                    cossSymbolBaseSymbolOrderBook.Asks.Remove(cossSymbolBaseSymbolBestAsk);
                                    cossSymbolBaseSymbolBestAsk = cossSymbolBaseSymbolOrderBook.BestAsk();
                                    cossSymbolBaseSymbolBestAskPrice = cossSymbolBaseSymbolBestAsk.Price;
                                }
                                else
                                {
                                    cossSymbolBaseSymbolBestAsk.Quantity -= quantity;
                                }

                                _log.Info($"Successfully placed a bid on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                            }
                            else
                            {
                                _log.Error($"Failed to place a bid on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                            }
                        }
                        catch (Exception exception)
                        {
                            _log.Error($"Failed to place a bid on Coss for {quantity} {symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                            _log.Error(exception);
                        }
                    }
                }

                decimal? bidPriceToPlace = null;
                decimal? baseSymbolToSpend = null;

                var optimalCossSymbolBaseSymbolBidPrice = MathUtil.ConstrainToMultipleOf(binanceSymbolBaseSymbolBestBidPrice * (100.0m - OptimalPercentDiff) / 100.0m, getPriceTick(symbol, baseSymbol));

                if (optimalCossSymbolBaseSymbolBidPrice > cossSymbolBaseSymbolBestBidPrice)
                {
                    bidPriceToPlace = optimalCossSymbolBaseSymbolBidPrice;
                    baseSymbolToSpend = optimalQuantityToSpendDictionary[baseSymbol];
                }
                else
                {
                    var tickUpBidPrice = cossSymbolBaseSymbolBestBidPrice + getPriceTick(symbol, baseSymbol);
                    var diff = binanceSymbolBaseSymbolBestBidPrice - tickUpBidPrice;
                    var ratio = diff / binanceSymbolBaseSymbolBestBidPrice;
                    var percentDiff = 100.0m * ratio;

                    if (percentDiff >= MinOpenBidPercentDiff)
                    {
                        bidPriceToPlace = tickUpBidPrice;
                        baseSymbolToSpend = nonOptimalQuantityToSpendDictioanry[baseSymbol];
                    }
                }


                if (bidPriceToPlace.HasValue && !baseSymbolsForKeptBids.Contains(baseSymbol) && !shouldWeAvoidPurchasing)
                {
                    var price = bidPriceToPlace.Value;
                    var availableBaseSymbol = balances.GetAvailableForSymbol(baseSymbol);

                    var baseSymbolQuantityToSpend = baseSymbolToSpend.Value;
                    if (baseSymbolQuantityToSpend >= 0.95m * availableBaseSymbol)
                    {
                        baseSymbolQuantityToSpend = 0.75m * availableBaseSymbol;
                    }

                    var symbolQuantityToBuy = baseSymbolQuantityToSpend / price;

                    var quantity = MathUtil.ConstrainToMultipleOf(symbolQuantityToBuy, getLotSize(symbol, baseSymbol));

                    _log.Info($"About to place a bid on Coss for {quantity} {symbol} at {price} {baseSymbol}.");

                    try
                    {
                        var orderResult = _exchangeClient.BuyLimitV2(IntegrationNameRes.Coss, symbol, baseSymbol, new QuantityAndPrice
                        {
                            Quantity = quantity,
                            Price = price
                        });

                        if (orderResult.WasSuccessful)
                        {
                            _log.Info($"Successfully placed a bid on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                        }
                        else
                        {
                            _log.Info($"Failed to place a bid on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place a bid on Coss for {quantity} {symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }

                decimal? askPriceToPlace = null;
                var optimalCossSymbolBaseSymbolAskPrice = MathUtil.ConstrainToMultipleOf(binanceSymbolBaseSymbolBestAskPrice * (100.0m + OptimalPercentDiff) / 100.0m,
                        getPriceTick(symbol, baseSymbol));

                if (optimalCossSymbolBaseSymbolAskPrice < cossSymbolBaseSymbolBestAskPrice)
                {
                    askPriceToPlace = optimalCossSymbolBaseSymbolAskPrice;
                    // TODO: determine the spend quantity.
                }
                else
                {
                    var tickDownAskPrice = cossSymbolBaseSymbolBestAskPrice - getPriceTick(symbol, baseSymbol);
                    var diff = cossSymbolBaseSymbolBestAskPrice - binanceSymbolBaseSymbolBestAskPrice;
                    var ratio = diff / cossSymbolBaseSymbolBestAskPrice;
                    var percentDiff = 100.0m * ratio;

                    if (percentDiff >= MinOpenAskPercentDiff)
                    {
                        askPriceToPlace = tickDownAskPrice;
                    }
                }

                if (askPriceToPlace.HasValue)
                {
                    askPricesToPlaceDictionary[baseSymbol] = askPriceToPlace.Value;
                }
            }

            var binanceSymbolBtcBestAsk = binanceSymbolBtcOrderBook.BestAsk();
            var binanceSymbolBtcBestAskPrice = binanceSymbolBtcBestAsk.Price;
            if (binanceSymbolBtcBestAskPrice <= 0) { throw new ApplicationException($"Binance {symbol}-BTC best ask price should be > 0."); }

            var binanceSymbolBtcBestBid = binanceSymbolBtcOrderBook.BestBid();
            var binanceSymbolBtcBestBidPrice = binanceSymbolBtcBestBid.Price;
            if (binanceSymbolBtcBestBidPrice <= 0) { throw new ApplicationException($"Binance {symbol}-BTC best bid price should be > 0."); }

            var binanceSymbolBtcAveragePrice = new List<decimal> { binanceSymbolBtcBestAskPrice, binanceSymbolBtcBestBidPrice }.Average();

            if (cossBaseSymbolsForSymbol.Contains("COSS"))
            {
                var cossCossEthOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, "COSS", "ETH", CachePolicy.ForceRefresh);
                var cossCossBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, "COSS", "BTC", CachePolicy.ForceRefresh);

                // SYMBOL-COSS
                var openOrders = GetOpenOrdersWithRetries(symbol, "COSS", CachePolicy.ForceRefresh) ?? new List<OpenOrderForTradingPair>();
                var openAsks = openOrders.Where(item => item.OrderType == OrderType.Ask).ToList();
                openAsks.ForEach(queryOpenOrder => CancelOpenOrderWithRetries(IntegrationNameRes.Coss, queryOpenOrder.OrderId));

                var cossSymbolCossOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, symbol, "COSS", CachePolicy.ForceRefresh);

                var openBids = openOrders.Where(item => item.OrderType == OrderType.Bid).ToList();
                OpenOrder existingOpenBid = null;
                if (openBids.Count >= 2)
                {
                    openBids.ForEach(queryOpenOrder => CancelOpenOrderWithRetries(IntegrationNameRes.Coss, queryOpenOrder.OrderId));
                    openBids.Clear();
                }
                else if (openBids.Count == 1)
                {
                    var openBid = openBids.Single();
                    var orderBookMatch = cossSymbolCossOrderBook.Bids.FirstOrDefault(queryOrder => queryOrder.Price == openBid.Price && queryOrder.Quantity == openBid.Quantity);
                    if (orderBookMatch == null)
                    {
                        CancelOpenOrderWithRetries(IntegrationNameRes.Coss, openBid.OrderId);
                        openBids.Clear();
                        cossSymbolCossOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, symbol, "COSS", CachePolicy.ForceRefresh);
                    }
                    else
                    {
                        existingOpenBid = openBid;
                        cossSymbolCossOrderBook.Bids.Remove(orderBookMatch);
                        availableBalances["COSS"] += openBid.Price * openBid.Quantity;
                    }
                }

                var cossBtcValue = DetermineCossBtcValue(new CossValuationData
                {
                    BinanceEthBtcOrderBook = binanceEthBtcOrderBook,
                    CossCossBtcOrderBook = cossCossBtcOrderBook,
                    CossCossEthOrderBook = cossCossEthOrderBook
                });

                var cossSymbolCossBestBid = cossSymbolCossOrderBook.BestBid();
                var cossSymbolCossBestBidPrice = cossSymbolCossBestBid.Price;
                if (cossSymbolCossBestBidPrice <= 0) { throw new ApplicationException($"Coss {symbol}-COSS best bid price should be > 0."); }

                var cossSymbolCossBestAsk = cossSymbolCossOrderBook.BestAsk();
                var cossSymbolCossBestAskPrice = cossSymbolCossBestAsk.Price;
                if (cossSymbolCossBestAskPrice <= 0) { throw new ApplicationException($"Coss {symbol}-COSS best ask price should be > 0."); }

                var binanceSymbolBtcAveragePriceAsCoss = binanceSymbolBtcAveragePrice / cossBtcValue;
                var optimalSymbolCossBidPrice = MathUtil.ConstrainToMultipleOf(binanceSymbolBtcAveragePriceAsCoss * (100.0m - OptimalPercentDiff) / 100.0m, getPriceTick(symbol, "COSS"));

                decimal? symbolCossPriceToBid = null;
                decimal? symbolCossQuantityToBid = null;
                if (optimalSymbolCossBidPrice > cossSymbolCossBestBidPrice)
                {
                    symbolCossPriceToBid = optimalSymbolCossBidPrice;
                    symbolCossQuantityToBid = OptimalQuantityToBuy;
                }
                else
                {
                    var tickUpPrice = cossSymbolCossBestBidPrice + getPriceTick(symbol, "COSS");
                    var tickUpPriceAsBtc = tickUpPrice * cossBtcValue;
                    var diff = binanceSymbolBtcBestBidPrice - tickUpPriceAsBtc;
                    var diffRatio = diff / tickUpPriceAsBtc;
                    var percentDiff = 100.0m * diffRatio;

                    if (percentDiff >= MinOpenBidPercentDiff)
                    {
                        symbolCossPriceToBid = tickUpPrice;
                        symbolCossQuantityToBid = OptimalQuantityToBuy * percentDiff / OptimalPercentDiff;
                    }
                }

                if (symbolCossPriceToBid.HasValue)
                {
                    symbolCossQuantityToBid = MathUtil.ConstrainToMultipleOf(symbolCossQuantityToBid.Value, getLotSize(symbol, "COSS"));

                    var maxAvailableQuantityToBid = availableBalances["COSS"] / symbolCossPriceToBid.Value / RoundingErrorPrevention;
                    if (symbolCossQuantityToBid.Value > maxAvailableQuantityToBid * 0.95m)
                    {
                        symbolCossQuantityToBid = MathUtil.ConstrainToMultipleOf(maxAvailableQuantityToBid * 0.95m, getLotSize(symbol, "COSS"));
                        if (symbolCossQuantityToBid.Value * symbolCossPriceToBid.Value < MinimumTradeCoss)
                        {
                            symbolCossQuantityToBid = null;
                        }
                    }
                }

                if (availableBalances["COSS"] < MinimumCossPar)
                {
                    symbolCossPriceToBid = null;
                    symbolCossQuantityToBid = null;
                }

                if (symbolCossPriceToBid.HasValue && !shouldWeAvoidPurchasing)
                {
                    var price = symbolCossPriceToBid.Value;

                    var quantity = symbolCossQuantityToBid.Value;
                    var baseSymbol = "COSS";

                    if (existingOpenBid != null && existingOpenBid.Price == price && MathUtil.IsWithinPercentDiff(existingOpenBid.Quantity, quantity, 5.0m))
                    {
                        _log.Info($"Our existing bid for {symbol}-{baseSymbol} already has the perfect price and quantity. Keeping it.");
                        availableBalances["COSS"] -= existingOpenBid.Price * existingOpenBid.Quantity;
                    }
                    else
                    {
                        if (existingOpenBid != null)
                        {
                            CancelOpenOrderWithRetries(IntegrationNameRes.Coss, existingOpenBid);
                        }

                        _log.Info($"About to place a bid on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                        try
                        {
                            var orderResult = _exchangeClient.BuyLimitV2(IntegrationNameRes.Coss, symbol, baseSymbol, new QuantityAndPrice
                            {
                                Quantity = quantity,
                                Price = price
                            });

                            if (orderResult.WasSuccessful)
                            {
                                availableBalances["COSS"] -= price * quantity;
                                _log.Info($"Successfully placed a bid on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                            }
                            else
                            {
                                _log.Info($"Failed to place a bid on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                            }
                        }
                        catch (Exception exception)
                        {
                            _log.Error(exception);
                        }
                    }
                }
                else if (existingOpenBid != null)
                {
                    var baseSymbol = "COSS";
                    _log.Info($"Cancelling our bid on Coss for {existingOpenBid.Quantity} {symbol} at {existingOpenBid.Price} {baseSymbol}.");
                    availableBalances["COSS"] -= existingOpenBid.Price * existingOpenBid.Quantity;

                    try
                    {
                        CancelOpenOrderWithRetries(IntegrationNameRes.Coss, existingOpenBid);
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to cancel our bid on Coss for {existingOpenBid.Quantity} {symbol} at {existingOpenBid.Price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }

                decimal? symbolCossPriceToAsk = null;
                var optimalSymbolCossAskPrice = MathUtil.RoundUp(binanceSymbolBtcAveragePriceAsCoss * (100.0m + OptimalPercentDiff) / 100.0m, MaxPriceDecimals);
                if (optimalSymbolCossAskPrice < cossSymbolCossBestAskPrice)
                {
                    symbolCossPriceToAsk = optimalSymbolCossAskPrice;
                }
                else
                {
                    var downTickSymbolCossAskPrice = cossSymbolCossBestAskPrice - getPriceTick(symbol, "COSS");
                    var diff = downTickSymbolCossAskPrice - binanceSymbolBtcAveragePriceAsCoss;
                    var diffRatio = diff / binanceSymbolBtcAveragePriceAsCoss;
                    var percentDiff = 100.0m * diffRatio;

                    if (percentDiff >= MinOpenAskPercentDiff)
                    {
                        symbolCossPriceToAsk = downTickSymbolCossAskPrice;
                    }
                }

                if (symbolCossPriceToAsk.HasValue)
                {
                    askPricesToPlaceDictionary["COSS"] = symbolCossPriceToAsk.Value;
                }
            }

            foreach (var altBase in new List<string> { "XRP", "USDT", "TUSD" })
            {
                try
                {
                    if (cossBaseSymbolsForSymbol.Contains(altBase))
                    {
                        var baseSymbol = altBase;

                        // SYMBOL-DOLLAR
                        OrderBook binanceUsdxBtcOrderBook = null;
                        var binanceUsdxBtcTask = LongRunningTask.Run(() =>
                        {
                            if (string.Equals(altBase, "USDT", StringComparison.InvariantCultureIgnoreCase))
                            {
                                var binanceOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, "BTC", altBase, CachePolicy.ForceRefresh); ;
                                binanceUsdxBtcOrderBook = InvertOrderBook(binanceOrderBook);
                            }
                            else
                            {
                                binanceUsdxBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, altBase, "BTC", CachePolicy.ForceRefresh);
                            }
                        });

                        var openOrders = GetOpenOrdersWithRetries(symbol, altBase, CachePolicy.ForceRefresh) ?? new List<OpenOrderForTradingPair>();
                        var openAsks = openOrders.Where(item => item.OrderType == OrderType.Ask).ToList();
                        var openBids = openOrders.Where(item => item.OrderType == OrderType.Bid).ToList();
                        openAsks.ForEach(queryOpenOrder => CancelOpenOrderWithRetries(IntegrationNameRes.Coss, queryOpenOrder.OrderId));
                        openAsks.Clear();

                        var cossSymbolUsdxOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, symbol, altBase, CachePolicy.ForceRefresh);

                        OpenOrder existingOpenBid = null;
                        if (openBids.Count >= 2)
                        {
                            openBids.ForEach(queryOpenOrder => CancelOpenOrderWithRetries(IntegrationNameRes.Coss, queryOpenOrder.OrderId));
                            openBids.Clear();

                            cossSymbolUsdxOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, symbol, altBase, CachePolicy.ForceRefresh);
                        }
                        else if (openBids.Count == 1)
                        {
                            var openBid = openBids.Single();
                            var matchingOrder = cossSymbolUsdxOrderBook.Bids.FirstOrDefault(queryOrder => queryOrder.Price == openBid.Price && queryOrder.Quantity == openBid.Quantity);
                            if (matchingOrder == null)
                            {
                                CancelOpenOrderWithRetries(IntegrationNameRes.Coss, openBid);
                                openBids.Clear();

                                cossSymbolUsdxOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, symbol, altBase, CachePolicy.ForceRefresh);
                            }
                            else
                            {
                                existingOpenBid = openBids.Single();

                                cossSymbolUsdxOrderBook.Bids.Remove(matchingOrder);
                                availableBalances[altBase] += existingOpenBid.Price * existingOpenBid.Quantity;
                            }
                        }

                        var cossSymbolUsdxBestAsk = cossSymbolUsdxOrderBook.BestAsk();
                        var cossSymbolUsdxBestAskPrice = cossSymbolUsdxBestAsk?.Price;
                        if (cossSymbolUsdxBestAskPrice <= 0) { throw new ApplicationException($"Coss {symbol}-{altBase} best ask price should be > 0."); }

                        var cossSymbolUsdxBestBid = cossSymbolUsdxOrderBook.BestBid();
                        var cossSymbolUsdxBestBidPrice = cossSymbolUsdxBestBid.Price;
                        if (cossSymbolUsdxBestBidPrice <= 0) { throw new ApplicationException($"Coss {symbol}-{altBase} best bid price should be > 0."); }

                        binanceUsdxBtcTask.Wait();
                        if(binanceUsdxBtcOrderBook == null || binanceUsdxBtcOrderBook.Asks == null || !binanceUsdxBtcOrderBook.Asks.Any()
                            || binanceUsdxBtcOrderBook.Bids == null || !binanceUsdxBtcOrderBook.Bids.Any())
                        {
                            continue;
                        }

                        var binanceUsdxBtcBestAskPrice = binanceUsdxBtcOrderBook.BestAsk().Price;
                        if (binanceUsdxBtcBestAskPrice <= 0) { throw new ApplicationException($"Binance {altBase}-BTC best ask price should be > 0."); }

                        var binanceUsdxBtcBestBidPrice = binanceUsdxBtcOrderBook.BestBid().Price;
                        if (binanceUsdxBtcBestBidPrice <= 0) { throw new ApplicationException($"Binance {altBase}-BTC best bid price should be > 0."); }

                        var usdxBtcRatio = new List<decimal> { binanceUsdxBtcBestAskPrice, binanceUsdxBtcBestBidPrice }.Average();

                        var binanceSymbolBtcBestBidPriceAsUsdx = binanceSymbolBtcBestBidPrice / usdxBtcRatio;
                        var instantBidDiff = binanceSymbolBtcBestBidPriceAsUsdx - cossSymbolUsdxBestAskPrice;
                        var instantBidRatio = instantBidDiff / cossSymbolUsdxBestAskPrice;
                        var instantBidPercentDiff = 100.0m * instantBidRatio;

                        if (instantBidPercentDiff >= MinTusdInstantBidPercentDiff && !shouldWeAvoidPurchasing)
                        {
                            var quantity = cossSymbolUsdxBestAsk.Quantity;
                            var price = cossSymbolUsdxBestAsk.Price;

                            _log.Info($"About to place a bid on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                            try
                            {
                                var orderResult = _exchangeClient.BuyLimitV2(IntegrationNameRes.Coss, symbol, baseSymbol, new QuantityAndPrice
                                {
                                    Quantity = quantity,
                                    Price = price
                                });

                                if (orderResult.WasSuccessful)
                                {
                                    availableBalances[altBase] -= price * quantity * 1.01m;

                                    cossSymbolUsdxOrderBook.Asks.Remove(cossSymbolUsdxBestAsk);
                                    cossSymbolUsdxBestAsk = cossSymbolUsdxOrderBook.BestAsk();
                                    cossSymbolUsdxBestAskPrice = cossSymbolUsdxBestAsk.Price;

                                    _log.Info($"Successfully placed a bid on Coss for {quantity} {symbol} at {price} {baseSymbol}.");

                                    // maybe it was the last ask in the order book?
                                    if (cossSymbolUsdxBestAskPrice <= 0) { return; }
                                }
                                else
                                {
                                    _log.Error($"Failed to place a bid on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                                }
                            }
                            catch (Exception exception)
                            {
                                _log.Error($"Failed to place a bid on Coss for {quantity} {symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                                _log.Error(exception);
                            }
                        }

                        var priceTick = getPriceTick(symbol, baseSymbol);
                        var optimalSymbolBtcBidPrice = binanceSymbolBtcBestBidPrice * (100.0m - OptimalPercentDiff) / 100.0m;
                        var optimalSymbolUsdxBidPrice = MathUtil.ConstrainToMultipleOf(optimalSymbolBtcBidPrice / usdxBtcRatio, priceTick);

                        decimal? symbolUsdxBidPriceToPlace = null;
                        decimal? usdxQuantityToSpend = null;

                        if (optimalSymbolUsdxBidPrice > cossSymbolUsdxBestBidPrice)
                        {
                            symbolUsdxBidPriceToPlace = optimalSymbolUsdxBidPrice;
                            usdxQuantityToSpend = OptimalDollarCoinQuantityToSpend;
                        }
                        else
                        {
                            var tickUpSymbolUsdxBidPrice = cossSymbolUsdxBestBidPrice + getPriceTick(symbol, altBase);
                            var tickUpSymbolUsdxBidPriceAsBtc = tickUpSymbolUsdxBidPrice * usdxBtcRatio;
                            var diff = binanceSymbolBtcBestBidPrice - tickUpSymbolUsdxBidPriceAsBtc;
                            var diffRatio = diff / binanceSymbolBtcBestBidPrice;
                            var percentDiff = 100.0m * diffRatio;

                            if (percentDiff >= MinOpenBidPercentDiff)
                            {
                                symbolUsdxBidPriceToPlace = tickUpSymbolUsdxBidPrice;
                                usdxQuantityToSpend = NonOptimalDollarCoinQuantityToSpend;
                            }
                        }

                        var shouldWeCancelOurExistingOpenBid = existingOpenBid != null;

                        if (symbolUsdxBidPriceToPlace.HasValue && !shouldWeAvoidPurchasing)
                        {
                            var price = MathUtil.ConstrainToMultipleOf(symbolUsdxBidPriceToPlace.Value, getPriceTick(symbol, baseSymbol));

                            var unconstrainedQuantity = usdxQuantityToSpend.Value / price;
                            if (usdxQuantityToSpend.Value > availableBalances[altBase] * 0.75m)
                            {
                                unconstrainedQuantity = availableBalances[altBase] / price * 0.75m;
                            }

                            var quantity = MathUtil.ConstrainToMultipleOf(unconstrainedQuantity, getLotSize(symbol, baseSymbol));
                            if (quantity * price >= (GetMinimumTradeForBaseSymbol(baseSymbol) ?? 0))
                            {
                                if (existingOpenBid != null && price == existingOpenBid.Price && MathUtil.IsWithinPercentDiff(quantity, existingOpenBid.Quantity, 5))
                                {
                                    _log.Info($"Keeping open bid on COSS for {existingOpenBid.Quantity} {symbol} at {existingOpenBid.Price} {baseSymbol}.");
                                    availableBalances[altBase] -= existingOpenBid.Quantity * existingOpenBid.Price;
                                    shouldWeCancelOurExistingOpenBid = false;
                                }
                                else
                                {
                                    if (shouldWeCancelOurExistingOpenBid)
                                    {
                                        CancelOpenOrderWithRetries(IntegrationNameRes.Coss, existingOpenBid);
                                        shouldWeCancelOurExistingOpenBid = false;
                                    }

                                    try
                                    {
                                        _log.Info($"About to place a bid on Coss for {quantity} {symbol} at {price} {baseSymbol}.");

                                        var orderResult = _exchangeClient.BuyLimitV2(IntegrationNameRes.Coss, symbol, baseSymbol, new QuantityAndPrice
                                        {
                                            Quantity = quantity,
                                            Price = price
                                        });

                                        if (orderResult.WasSuccessful)
                                        {
                                            availableBalances[altBase] -= quantity * price;
                                            _log.Info($"Successfully placed a bid on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                                        }
                                        else
                                        {
                                            _log.Error($"Failed to place a bid on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                                        }
                                    }
                                    catch (Exception exception)
                                    {
                                        _log.Error($"Failed to place a bid on Coss for {quantity} {symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                                        _log.Error(exception);
                                    }
                                }
                            }
                        }

                        if (shouldWeCancelOurExistingOpenBid)
                        {
                            CancelOpenOrderWithRetries(IntegrationNameRes.Coss, existingOpenBid);
                            shouldWeCancelOurExistingOpenBid = false;
                        }

                        decimal? symbolUsdxPriceToAsk = null;
                        var binanceSymbolBtcBestAskPriceAsUsdx = binanceSymbolBtcBestAskPrice / usdxBtcRatio;

                        var didWeDoAnInstantSell = false;
                        binanceSymbolBtcBestBidPriceAsUsdx = binanceSymbolBtcBestBidPrice / usdxBtcRatio;
                        {
                            var diff = cossSymbolUsdxBestBidPrice - binanceSymbolBtcBestBidPriceAsUsdx;
                            var diffRatio = diff / binanceSymbolBtcBestBidPriceAsUsdx;
                            var percentDiff = 100.0m * diffRatio;
                            if (percentDiff >= DollarCoinMinInstantSellProfitPercent)
                            {
                                var price = cossSymbolUsdxBestBidPrice;
                                // var potentialQuantity = cossSymbolTusdBestBid.Quantity;
                                decimal quantity = 0;

                                var tradingPair = getTradingPair(symbol, baseSymbol);

                                // TODO: need to make sure this is at least the min quantity.
                                if (cossSymbolUsdxBestBid.Quantity <= availableBalances[symbol])
                                {
                                    quantity = cossSymbolUsdxBestBid.Quantity;
                                }
                                else
                                {
                                    quantity = MathUtil.ConstrainToMultipleOf(availableBalances[symbol], getLotSize(symbol, baseSymbol));
                                }

                                if (quantity > 0)
                                {
                                    didWeDoAnInstantSell = true;
                                    try
                                    {
                                        _log.Info($"About to place an ask on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                                        var orderResult = _exchangeClient.SellLimitV2(IntegrationNameRes.Coss, symbol, baseSymbol, new QuantityAndPrice
                                        {
                                            Price = price,
                                            Quantity = quantity
                                        });

                                        if (orderResult.WasSuccessful)
                                        {
                                            _log.Info($"Successfully placed an ask on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                                            availableBalances[symbol] -= price * quantity * 1.01m;
                                        }
                                        else
                                        {
                                            _log.Error($"Failed to place an ask on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                                        }
                                    }
                                    catch (Exception exception)
                                    {
                                        _log.Error($"Failed to place an ask on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                                        _log.Error(exception);
                                    }
                                }
                            }
                        }

                        if (!didWeDoAnInstantSell)
                        {
                            var optimalSymbolTusdAskPrice = MathUtil.ConstrainToMultipleOf(binanceSymbolBtcBestAskPriceAsUsdx * (100.0m + OptimalPercentDiff) / 100.0m, priceTick);
                            if (optimalSymbolTusdAskPrice < cossSymbolUsdxBestAskPrice)
                            {
                                symbolUsdxPriceToAsk = optimalSymbolTusdAskPrice;
                            }
                            else
                            {
                                var downTickSymbolUsdxAskPrice = cossSymbolUsdxBestAskPrice - getPriceTick(symbol, altBase);
                                var diff = downTickSymbolUsdxAskPrice - binanceSymbolBtcBestAskPriceAsUsdx;
                                var diffRatio = diff / binanceSymbolBtcBestAskPriceAsUsdx;
                                var percentDiff = 100.0m * diffRatio;

                                if (percentDiff >= MinOpenAskPercentDiff)
                                {
                                    symbolUsdxPriceToAsk = downTickSymbolUsdxAskPrice;
                                }
                            }

                            if (symbolUsdxPriceToAsk.HasValue)
                            {
                                askPricesToPlaceDictionary[altBase] = symbolUsdxPriceToAsk.Value;
                            }
                        }
                    }
                }
                catch(Exception exception)
                {
                    _log.Error(exception);
                }
             }


            if (askPricesToPlaceDictionary.Keys.Any())
            {
                const decimal feeFactor = 0.9979m;
                var quantityToSell = availableBalances[symbol] / askPricesToPlaceDictionary.Keys.Count / RoundingErrorPrevention 
                    // * binanceSymbolBtcAveragePrice 
                    * feeFactor;

                foreach (var baseSymbol in askPricesToPlaceDictionary.Keys)
                {
                    var quantity = MathUtil.ConstrainToMultipleOf(quantityToSell, getLotSize(symbol, baseSymbol));
                    var price = askPricesToPlaceDictionary[baseSymbol];

                    var transactionQuantity = quantity * price;
                    var minimumTradeQuantity = minimumTransactionQuantityDictionary[baseSymbol];
                    if (transactionQuantity >= minimumTradeQuantity)
                    {
                        try
                        {
                            _log.Info($"About to place an ask on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                            var orderResult = _exchangeClient.SellLimitV2(IntegrationNameRes.Coss, symbol, baseSymbol, new QuantityAndPrice
                            {
                                Quantity = quantity,
                                Price = price
                            });

                            if (orderResult.WasSuccessful)
                            {
                                _log.Info($"Successfully placed an ask on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                            }
                            else
                            {
                                _log.Error($"Failed to place an ask on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                            }
                        }
                        catch (Exception exception)
                        {
                            _log.Error($"Failed to place an ask on Coss for {quantity} {symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                            _log.Error(exception);
                        }
                    }
                }
            }
        }
    }
}
