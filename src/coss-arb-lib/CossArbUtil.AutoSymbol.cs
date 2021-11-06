using cache_lib.Models;
using math_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using task_lib;
using trade_constants;
using trade_model;

namespace coss_arb_lib
{
    public partial class CossArbUtil
    {
        public void AutoSymbol(
            string symbol,
            string compExchange)
        {
            try
            {
                _log.Debug($"Beginning CossArbUtil.AutoSymol({symbol})");

                var isCompHighVolume = string.Equals(compExchange, IntegrationNameRes.Binance, StringComparison.InvariantCultureIgnoreCase);

                // TODO: This needs to move into the coss arb config.
                decimal InstantBuyMinimumPercentDiff = isCompHighVolume ? 4.0m : 8.0m;
                decimal OpenBidMinimumPercentDiff = isCompHighVolume ? 6.0m : 7.0m;
                decimal OpenAskMinimumPercentDiff = isCompHighVolume ? 3.5m : 5.0m;
                decimal InstantSellMinimumPercentDiff = isCompHighVolume ? 0 : 4.0m;

                var cossAgentConfig = _configClient.GetCossAgentConfig();
                if (cossAgentConfig == null) { throw new ApplicationException("Failed to retrieve coss agent config."); }
                if (!cossAgentConfig.IsCossAutoTradingEnabled)
                {
                    _log.Info("Coss auto trading is disabled. Not running the coss eth-btc workflow.");
                    return;
                }

                var cossTradingPairs = GetTradingPairsWithRetries(IntegrationNameRes.Coss, CachePolicy.AllowCache);

                var getPriceTick = new Func<string, string, decimal>((argSymbol, argBaseSymbol) =>
                {
                    var matchingPair = cossTradingPairs.FirstOrDefault(item => string.Equals(item.Symbol, argSymbol, StringComparison.InvariantCultureIgnoreCase)
                        && string.Equals(item.BaseSymbol, argBaseSymbol, StringComparison.InvariantCultureIgnoreCase));

                    return (matchingPair?.PriceTick) ?? DefaultPriceTick;
                });

                var lotSize = cossTradingPairs.Where(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase))
                    .Select(item => item.LotSize)
                    .Where(item => item.HasValue)
                    .FirstOrDefault();

                // var cossVsEthOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Coss, "COS", "ETH", CachePolicy.AllowCache);
                var cossVsBtcOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Coss, "COS", "BTC", CachePolicy.AllowCache);

                var cossVsBtcBestBidPrice = cossVsBtcOrderBook.BestBid().Price;
                var cossVsBtcBestAskPrice = cossVsBtcOrderBook.BestAsk().Price;

                var comparableBaseSymbols = new List<string> {
                    "ETH",
                    "BTC",
                    "COS",
                    "USDT",
                    "TUSD"
                };

                var cossBaseSymbols =
                    cossTradingPairs
                        .Where(item =>
                        string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                        && comparableBaseSymbols.Any(interestingBaseSymbol =>
                        string.Equals(interestingBaseSymbol, item.BaseSymbol, StringComparison.InvariantCultureIgnoreCase)))
                        .Select(item => item.BaseSymbol)
                            .ToList();

                if (cossBaseSymbols == null || !cossBaseSymbols.Any())
                {
                    _log.Error($"Coss does not have any trading pairs for {symbol}");
                    return;
                }

                var isCossABaseSymbol = cossBaseSymbols.Any(queryCossBaseSymbol => string.Equals(queryCossBaseSymbol, "COS", StringComparison.InvariantCultureIgnoreCase));

                var compBaseSymbols = GetTradingPairsWithRetries(compExchange, CachePolicy.AllowCache)
                        .Where(item =>
                        string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                        && comparableBaseSymbols.Any(interestingBaseSymbol =>
                        string.Equals(interestingBaseSymbol, item.BaseSymbol, StringComparison.InvariantCultureIgnoreCase)))
                        .Select(item => item.BaseSymbol)
                            .ToList();

                if (compBaseSymbols == null || !compBaseSymbols.Any())
                {
                    _log.Error($"{compExchange} does not have any trading pairs for {symbol}");
                    return;
                }

                decimal? symbolUsdValue = null;
                decimal ethUsdValue = 0;
                decimal btcUsdValue = 0;

                var valuationTask = LongRunningTask.Run(() =>
                {
                    ethUsdValue = _workflowClient.GetUsdValue("ETH", CachePolicy.AllowCache).Value;
                    btcUsdValue = _workflowClient.GetUsdValue("BTC", CachePolicy.AllowCache).Value;
                    symbolUsdValue = _workflowClient.GetUsdValue(symbol, CachePolicy.AllowCache);
                });

                List<(OrderBook OrderBook, string BaseSymbol)> cossOrderBooks = cossBaseSymbols.Select(queryBaseSymbol =>
                    (_exchangeClient.GetOrderBook(IntegrationNameRes.Coss, symbol, queryBaseSymbol, CachePolicy.ForceRefresh),
                    queryBaseSymbol))
                    .ToList();

                // Cancel any existing open orders.
                foreach (var baseSymbol in cossBaseSymbols)
                {
                    var existingCossOpenOrders = GetOpenOrdersWithRetries(symbol, baseSymbol, CachePolicy.ForceRefresh);
                    foreach (var openOrder in existingCossOpenOrders ?? new List<OpenOrderForTradingPair>())
                    {
                        CancelOpenOrderWithRetries(IntegrationNameRes.Coss, openOrder.OrderId);
                    }
                }

                List<(OrderBook OrderBook, string BaseSymbol)> compOrderBooks = compBaseSymbols.Select(queryBaseSymbol =>
                    (_exchangeClient.GetOrderBook(compExchange, symbol, queryBaseSymbol, CachePolicy.ForceRefresh),
                    queryBaseSymbol))
                    .ToList();

                var balances = _exchangeClient.GetBalances(IntegrationNameRes.Coss, CachePolicy.ForceRefresh);

                OrderBook cossBtcOrderBook = null;
                if (isCossABaseSymbol)
                {
                    cossBtcOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Coss, "COS", "BTC", CachePolicy.ForceRefresh);
                }

                valuationTask.Wait();

                var cossAggregateOrderBook = GenerateAggregateOrderBook(
                    cossOrderBooks.Select(queryCossOrderBook =>
                    new OrderBookAndBaseSymbol
                    {
                        OrderBook = queryCossOrderBook.OrderBook,
                        BaseSymbol = queryCossOrderBook.BaseSymbol
                    }).ToList(),
                    new List<SymbolAndUsdValue>
                    {
                    new SymbolAndUsdValue { Symbol = "ETH", UsdValue = ethUsdValue },
                    new SymbolAndUsdValue { Symbol = "BTC", UsdValue = btcUsdValue },
                    new SymbolAndUsdValue { Symbol = "USDT", UsdValue = 1 },
                    }, cossBtcOrderBook);

                var compAggregateOrderBook = GenerateAggregateOrderBook(
                    compOrderBooks.Select(queryCompOrderBook =>
                    new OrderBookAndBaseSymbol
                    {
                        OrderBook = queryCompOrderBook.OrderBook,
                        BaseSymbol = queryCompOrderBook.BaseSymbol
                    }).ToList(),
                    new List<SymbolAndUsdValue>
                    {
                    new SymbolAndUsdValue { Symbol = "ETH", UsdValue = ethUsdValue },
                    new SymbolAndUsdValue { Symbol = "BTC", UsdValue = btcUsdValue },
                    new SymbolAndUsdValue { Symbol = "USDT", UsdValue = 1 },
                    }, cossBtcOrderBook);

                var cossAsks = cossAggregateOrderBook.Where(item => item.OrderType == OrderType.Ask).ToList();
                var cossBids = cossAggregateOrderBook.Where(item => item.OrderType == OrderType.Bid).ToList();

                var bestCossBid = cossBids.OrderByDescending(item => item.UsdPrice).FirstOrDefault();
                if (bestCossBid == null) { return; }
                var bestCossBidUsdPrice = bestCossBid.UsdPrice;
                var bestCossAsk = cossAsks.OrderBy(item => item.UsdPrice).FirstOrDefault();
                if (bestCossAsk == null) { return; }
                var bestCossAskUsdPrice = bestCossAsk.UsdPrice;

                var compAsks = compAggregateOrderBook.Where(item => item.OrderType == OrderType.Ask).ToList();
                var compBids = compAggregateOrderBook.Where(item => item.OrderType == OrderType.Bid).ToList();

                var bestCompBid = compBids.OrderByDescending(item => item.UsdPrice).FirstOrDefault();
                if (bestCompBid == null) { return; }
                var bestCompBidUsdPrice = bestCompBid.UsdPrice;
                var bestCompAsk = compAsks.OrderBy(item => item.UsdPrice).FirstOrDefault();
                if (bestCompAsk == null) { return; }
                var bestCompAskUsdPrice = bestCompAsk.UsdPrice;

                // Instant buy
                var viableCossAsks = cossAsks.
                    Where(cossAsk =>
                    {
                        if (symbolUsdValue.HasValue && cossAsk.UsdPrice >= symbolUsdValue) { return false; }
                        var diff = bestCompBidUsdPrice - cossAsk.UsdPrice;
                        var ratio = diff / cossAsk.UsdPrice;
                        var percentDiff = 100.0m * ratio;
                        return percentDiff >= InstantBuyMinimumPercentDiff;
                    })
                    .ToList();

                if (viableCossAsks.Any())
                {
                    foreach (var baseSymbol in cossBaseSymbols)
                    {
                        var viableCossAsksForBaseSymbol = viableCossAsks
                            .Where(item =>
                            string.Equals(item.BaseSymbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase)).ToList();

                        if (viableCossAsksForBaseSymbol.Any())
                        {
                            var worstNativeAskPriceToTakeForBaseSymbol = viableCossAsksForBaseSymbol.OrderByDescending(item => item.NativePrice).First().NativePrice;
                            var quantityToTakeForBaseSymbol = viableCossAsksForBaseSymbol.Sum(item => item.Quantity);

                            var expectedTradeQuantity = worstNativeAskPriceToTakeForBaseSymbol * quantityToTakeForBaseSymbol;

                            var nullableMinimumTrade = GetMinimumTradeForBaseSymbol(baseSymbol);
                            if (nullableMinimumTrade.HasValue && expectedTradeQuantity < nullableMinimumTrade.Value)
                            {
                                var minimumTrade = nullableMinimumTrade.Value;
                                quantityToTakeForBaseSymbol = minimumTrade / worstNativeAskPriceToTakeForBaseSymbol;
                            }

                            if (lotSize.HasValue)
                            {
                                if (lotSize.Value == 1)
                                {
                                    quantityToTakeForBaseSymbol = (int)quantityToTakeForBaseSymbol;
                                }
                            }

                            if (string.Equals(compExchange, "binance", StringComparison.InvariantCultureIgnoreCase) && quantityToTakeForBaseSymbol > 0)
                            {
                                _log.Info($"About to place a limit buy order for {quantityToTakeForBaseSymbol} {symbol} at {worstNativeAskPriceToTakeForBaseSymbol} {baseSymbol} on Coss.");
                                var buyLimitResult = _exchangeClient.BuyLimit(IntegrationNameRes.Coss, symbol, baseSymbol, new QuantityAndPrice
                                {
                                    Price = worstNativeAskPriceToTakeForBaseSymbol,
                                    Quantity = quantityToTakeForBaseSymbol
                                });

                                if (buyLimitResult)
                                {
                                    _log.Info($"Successfully placed a limit buy order for {quantityToTakeForBaseSymbol} {symbol} at {worstNativeAskPriceToTakeForBaseSymbol} {baseSymbol} on Coss.");
                                }
                                else
                                {
                                    _log.Error($"Failed to buy {quantityToTakeForBaseSymbol} {symbol} at {worstNativeAskPriceToTakeForBaseSymbol} {baseSymbol} on Coss.");
                                }
                            }
                        }
                    }
                }

                // determine the viable coss bids.
                var viableCossBids = new List<AggregateOrderBookItem>();
                foreach (var cossBid in cossBids)
                {
                    var diff = cossBid.UsdPrice - bestCompAskUsdPrice;
                    var ratio = diff / bestCompAskUsdPrice;
                    var percentDiff = 100.0m * ratio;
                    if (percentDiff >= InstantSellMinimumPercentDiff)
                    {
                        if (symbolUsdValue.HasValue && cossBid.UsdPrice < symbolUsdValue)
                        {
                            Console.WriteLine("Would have taken this bid, but it's below the valuation price.");
                            continue;
                        }

                        viableCossBids.Add(cossBid);
                    }
                }

                var symbolBalance = balances.GetHoldingForSymbol(symbol);
                var symbolBalanceAvailable = symbolBalance?.Available ?? 0;
                var baseSymbolsThatWeSoldAgainst = new List<string>();
                if (viableCossBids.Any())
                {
                    foreach (var baseSymbol in cossBaseSymbols)
                    {
                        var viableCossBidsForBaseSymbol = viableCossBids.Where(item => string.Equals(item.BaseSymbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase)).ToList();

                        if (viableCossBidsForBaseSymbol.Any())
                        {
                            var worstNativeBidPriceToTakeForBaseSymbol = viableCossBidsForBaseSymbol.OrderBy(item => item.NativePrice).First().NativePrice;
                            var priceToSellAt = worstNativeBidPriceToTakeForBaseSymbol / RoundingErrorPrevention;
                            var quantityToSell = viableCossBidsForBaseSymbol.Sum(item => item.Quantity) * RoundingErrorPrevention;

                            var baseSymbolTradeQuantity = priceToSellAt * quantityToSell;
                            var minimumTradeForBaseSymbol = GetMinimumTradeForBaseSymbol(baseSymbol) ?? 0;

                            if (baseSymbolTradeQuantity < minimumTradeForBaseSymbol)
                            {
                                quantityToSell = minimumTradeForBaseSymbol / worstNativeBidPriceToTakeForBaseSymbol * RoundingErrorPrevention;
                                if (quantityToSell > symbolBalanceAvailable)
                                {
                                    quantityToSell = 0;
                                }
                            }
                            else if (quantityToSell > symbolBalanceAvailable * 0.998m)
                            {
                                quantityToSell = symbolBalanceAvailable * 0.998m;
                                var updatedBaseSymbolTradeQuantity = quantityToSell * priceToSellAt;
                                if (updatedBaseSymbolTradeQuantity < minimumTradeForBaseSymbol)
                                {
                                    quantityToSell = 0;
                                }
                            }

                            if (lotSize.HasValue && lotSize.Value > 0)
                            {
                                quantityToSell = MathUtil.ConstrainToMultipleOf(quantityToSell, lotSize.Value);
                            }

                            if (quantityToSell > 0)
                            {
                                _log.Info($"About to place a sell limit order for {quantityToSell} {symbol} at {worstNativeBidPriceToTakeForBaseSymbol} {baseSymbol}");
                                try
                                {
                                    baseSymbolsThatWeSoldAgainst.Add(baseSymbol);
                                    var placeOrderResult = _exchangeClient.SellLimit(IntegrationNameRes.Coss, symbol, baseSymbol, new QuantityAndPrice
                                    {
                                        Quantity = quantityToSell,
                                        Price = priceToSellAt
                                    });

                                    if (placeOrderResult)
                                    {
                                        _log.Error($"Successfully placed a sell limit order for {quantityToSell} {symbol} at {worstNativeBidPriceToTakeForBaseSymbol} {baseSymbol}");
                                    }
                                    else
                                    {
                                        _log.Error($"Failed to sell {quantityToSell} {symbol} at {worstNativeBidPriceToTakeForBaseSymbol} {baseSymbol}");
                                    }
                                }
                                catch (Exception exception)
                                {
                                    _log.Error($"Failed to sell {quantityToSell} {symbol} at {worstNativeBidPriceToTakeForBaseSymbol} {baseSymbol}");
                                    _log.Error(exception);
                                }

                                var openOrdersAfterSales = GetOpenOrdersWithRetries(symbol, baseSymbol, CachePolicy.ForceRefresh);
                                if (openOrdersAfterSales != null && openOrdersAfterSales.Any())
                                {
                                    // Make sure that we don't violate the TOS by cancelling an order we just placed too soon.
                                    Thread.Sleep(TimeSpan.FromSeconds(11));

                                    foreach (var openOrder in openOrdersAfterSales)
                                    {
                                        try
                                        {
                                            _exchangeClient.CancelOrder(IntegrationNameRes.Coss, openOrder.OrderId);
                                        }
                                        catch (Exception exception)
                                        {
                                            _log.Error(exception);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // now let's refresh the data
                List<(OrderBook OrderBook, string BaseSymbol)> updatedCossOrderBookCombos = cossBaseSymbols.Select(queryBaseSymbol =>
                    (_exchangeClient.GetOrderBook(IntegrationNameRes.Coss, symbol, queryBaseSymbol, CachePolicy.ForceRefresh),
                    queryBaseSymbol))
                    .ToList();

                var updatedBalances = _exchangeClient.GetBalances(IntegrationNameRes.Coss, CachePolicy.ForceRefresh);
                var updatedSymbolBalance = updatedBalances.GetHoldingForSymbol(symbol);
                var updatedAvailableSymbolBalance = updatedSymbolBalance.Available;

                if (updatedAvailableSymbolBalance > 0)
                {
                    var bestEffectiveCompBidPriceDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
                    {
                        { "ETH", (bestCompBidUsdPrice / ethUsdValue) },
                        { "BTC", (bestCompBidUsdPrice / btcUsdValue) },
                        { "COS", (bestCompBidUsdPrice / cossVsBtcBestBidPrice / btcUsdValue) }
                    };

                    var bestEffectiveCompAskPriceDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
                    {
                        { "ETH", (bestCompAskUsdPrice / ethUsdValue) },
                        { "BTC", (bestCompAskUsdPrice / btcUsdValue) },
                        { "COS", (bestCompBidUsdPrice / cossVsBtcBestAskPrice / btcUsdValue) }
                    };

                    var symbolsWithRoomForAnAskAndData = new List<(string BaseSymbol, decimal AskPrice)>();
                    foreach (var updatedCossOrderBookCombo in updatedCossOrderBookCombos)
                    {
                        var updatedCossOrderBook = updatedCossOrderBookCombo.OrderBook;
                        var baseSymbol = updatedCossOrderBookCombo.BaseSymbol;

                        var targetSymbolAskPrice = updatedCossOrderBook.BestAsk().Price * 0.9999m;
                        if (!bestEffectiveCompAskPriceDictionary.ContainsKey(baseSymbol)) { continue; }

                        var bestEffectCompAskSymbolPrice = bestEffectiveCompAskPriceDictionary[baseSymbol];

                        var symbolDiff = targetSymbolAskPrice - bestEffectCompAskSymbolPrice;
                        var symbolDiffRatio = symbolDiff / bestEffectCompAskSymbolPrice;

                        // 7.5%
                        var isThereRoomForASymbolAsk = symbolDiffRatio >= 0.075m;

                        if (isThereRoomForASymbolAsk)
                        {
                            symbolsWithRoomForAnAskAndData.Add(
                            (
                                baseSymbol,
                                targetSymbolAskPrice
                            ));
                        }
                    }

                    for (var i = 0; i < symbolsWithRoomForAnAskAndData.Count; i++)
                    {
                        var symbolAndData = symbolsWithRoomForAnAskAndData[i];
                        var baseSymbol = symbolAndData.BaseSymbol;
                        var askPrice = symbolAndData.AskPrice;
                        var quantityToSell = updatedAvailableSymbolBalance / RoundingErrorPrevention / (symbolsWithRoomForAnAskAndData.Count)
                            * 0.99m;
                        //* 0.997m;

                        // TODO: this is a really crappy way to do this...
                        if (lotSize.HasValue && lotSize.Value > 0)
                        {
                            // if (lotSize.Value == 1) { quantityToSell = (int)quantityToSell; }
                            quantityToSell = MathUtil.ConstrainToMultipleOf(quantityToSell, lotSize.Value);
                        }

                        var priceTick = getPriceTick(symbol, baseSymbol);
                        if (priceTick > 0)
                        {
                            askPrice = MathUtil.ConstrainToMultipleOf(askPrice, priceTick);
                        }

                        if (quantityToSell * askPrice < MinimumTradeDictionary[baseSymbol])
                        {
                            continue;
                        }

                        if (quantityToSell > 0)
                        {
                            if (!string.Equals(baseSymbol, "COS", StringComparison.InvariantCultureIgnoreCase))
                            {
                                try
                                {
                                    var sellLimitResult = _exchangeClient.SellLimit(IntegrationNameRes.Coss, symbol, baseSymbol, new QuantityAndPrice
                                    {
                                        Price = askPrice,
                                        Quantity = quantityToSell
                                    });

                                    if (!sellLimitResult)
                                    {
                                        _log.Error($"Failed to place a sell limit order for {quantityToSell} {symbol} at {askPrice} {baseSymbol}.");
                                    }
                                }
                                catch (Exception exception)
                                {
                                    _log.Error($"Failed to place a sell limit order for {quantityToSell} {symbol} at {askPrice} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                                    _log.Error(exception);
                                }
                            }
                        }
                    }
                }

                if (string.Equals(compExchange, "binance", StringComparison.InvariantCultureIgnoreCase))
                {

                    updatedCossOrderBookCombos = cossBaseSymbols.Select(queryBaseSymbol =>
                    (_exchangeClient.GetOrderBook(IntegrationNameRes.Coss, symbol, queryBaseSymbol, CachePolicy.ForceRefresh),
                    queryBaseSymbol))
                    .ToList();

                    updatedBalances = _exchangeClient.GetBalances(IntegrationNameRes.Coss, CachePolicy.ForceRefresh);
                    updatedSymbolBalance = updatedBalances.GetHoldingForSymbol(symbol);
                    updatedAvailableSymbolBalance = updatedSymbolBalance.Available;

                    var symbolValueDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
                {
                    { "ETH", ethUsdValue },
                    { "BTC", btcUsdValue }
                };

                    foreach (var combo in updatedCossOrderBookCombos)
                    {
                        var baseSymbol = combo.BaseSymbol;
                        var baseSymbolValue = string.Equals(baseSymbol, "COS", StringComparison.InvariantCultureIgnoreCase)
                            ? cossVsBtcBestBidPrice * btcUsdValue
                            : symbolValueDictionary[baseSymbol];

                        var updatedBestSymbolBid = combo.OrderBook.BestBid();
                        var updatedBestSymbolBidPrice = updatedBestSymbolBid.Price;
                        var updatedBestSymbolBidPriceAsUsd = updatedBestSymbolBidPrice * baseSymbolValue;

                        var potentialSymbolBid = (updatedBestSymbolBidPrice * 1.0001m) + 0.00000001m;
                        var potentialSymbolBidAsUsd = potentialSymbolBid * baseSymbolValue;

                        var compSymbolProfit = bestCompBidUsdPrice - potentialSymbolBidAsUsd;
                        var compSymbolProfitRatio = compSymbolProfit / potentialSymbolBidAsUsd;
                        var compEthProfitPercentage = 100.0m * compSymbolProfitRatio;
                        var valuationSymbolProfit = symbolUsdValue - potentialSymbolBidAsUsd;
                        var valuationSymbolProfitRatio = valuationSymbolProfit / potentialSymbolBidAsUsd;
                        var valuationSymbolProfitPercentage = 100.0m * valuationSymbolProfitRatio;

                        if (valuationSymbolProfitPercentage >= OpenBidMinimumPercentDiff
                            && valuationSymbolProfitPercentage >= OpenBidMinimumPercentDiff)
                        {
                            var baseSymbolQuantity = BaseSymbolOpenQuantityDictionary.ContainsKey(baseSymbol)
                                ? BaseSymbolOpenQuantityDictionary[baseSymbol]
                                : 0.1m;

                            var symbolQuantity = baseSymbolQuantity / potentialSymbolBid;

                            try
                            {
                                var buyLimitResult = _exchangeClient.BuyLimit(IntegrationNameRes.Coss, symbol, baseSymbol, new QuantityAndPrice
                                {
                                    Quantity = symbolQuantity,
                                    Price = potentialSymbolBid
                                });

                                if (!buyLimitResult)
                                {
                                    _log.Error($"Failed to buy {baseSymbolQuantity} {symbol} at {potentialSymbolBid} {baseSymbol}.");
                                }
                            }
                            catch (Exception exception)
                            {
                                _log.Error($"Failed to buy {baseSymbolQuantity} {symbol} at {potentialSymbolBid} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                                _log.Error(exception);
                            }
                        }
                    }
                }
            }
            finally
            {
                _log.Debug($"Done with CossArbUtil.AutoSymol({symbol})");
            }
        }
    }
}
