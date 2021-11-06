using arb_workflow_lib.Models;
using cache_lib.Models;
using math_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using task_lib;
using trade_constants;
using trade_model;

namespace arb_workflow_lib
{
    public partial class ArbWorkflowUtil
    {
        public void AutoSymbol(
            string symbol,
            string arbExchange,
            string compExchange,
            string altBaseSymbol = null,
            bool waiveArbDepositAndWithdrawalCheck = false,
            bool waiveCompDepositAndWithdrawalCheck = false,
            decimal? maxUsdValueToOwn = null,
            decimal? idealPercentDiff = null,
            Dictionary<string, decimal> openBidQuantityOverride = null)
        {
            _log.Info($"AutoSymbol {symbol} {arbExchange} {compExchange}");

            var effectiveOpenBidQuantityDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var key in BaseSymbolOpenBidQuantityDictionary.Keys) { effectiveOpenBidQuantityDictionary[key] = BaseSymbolOpenBidQuantityDictionary[key]; }
            if (openBidQuantityOverride != null)
            {
                foreach (var key in openBidQuantityOverride.Keys) { effectiveOpenBidQuantityDictionary[key] = openBidQuantityOverride[key]; }
            }

            const decimal DefaultLotSize = 0.00000001m;
            const decimal DefaultPriceTick = 0.00000001m;

            var effectiveIdealPercentDiff = idealPercentDiff.HasValue && idealPercentDiff > 0
                ? idealPercentDiff.Value
                : 15.0m;

            var effectiveOpenBidMinPercentDiff = OpenBidMinimumPercentDiff >= 1.1m * effectiveIdealPercentDiff
                ? effectiveIdealPercentDiff / 1.1m
                : OpenBidMinimumPercentDiff;

            var effectiveOpenAskMinPercentDiff = OpenAskMinimumPercentDiff >= 1.1m * effectiveIdealPercentDiff
                ? effectiveIdealPercentDiff / 1.1m
                : OpenAskMinimumPercentDiff;

            var isCompExchangeHighVolume = string.Equals(compExchange, IntegrationNameRes.Binance, StringComparison.InvariantCultureIgnoreCase);

            _log.Verbose($"Beginning AutoSymbol for Symbol {symbol}; Exchange: {arbExchange}; CompExchange: {compExchange}; AltBaseSymbol: {altBaseSymbol}.");

            List<TradingPair> arbTradingPairs = null;
            List<TradingPair> compTradingPairs = null;
            List<CommodityForExchange> arbCommodities = null;
            List<CommodityForExchange> compCommodities = null;

            _log.Verbose($"Getting the trading pairs for {arbExchange} and {compExchange}...");
            var compTradingPairsTask = LongRunningTask.Run(() =>
            {
                compTradingPairs = GetTradingPairsWithRetries(compExchange, CachePolicy.AllowCache);
                compCommodities = GetCommoditiesWithRetries(compExchange, CachePolicy.AllowCache);
            });

            var arbTradingPairsTask = LongRunningTask.Run(() =>
            {
                arbTradingPairs = GetTradingPairsWithRetries(arbExchange, CachePolicy.AllowCache);
                arbCommodities = GetCommoditiesWithRetries(arbExchange, CachePolicy.AllowCache);
            });

            compTradingPairsTask.Wait();
            _log.Verbose($"  Done getting the trading pairs and commodities for {compExchange}...");

            arbTradingPairsTask.Wait();
            _log.Verbose($"  Done getting the trading pairs and commodities for {arbExchange}...");

            var getLotSize = new Func<string, string, decimal>((argSymbol, argBaseSymbol) =>
            {
                var matchingTradingPair = arbTradingPairs.SingleOrDefault(queryTradingPair => string.Equals(queryTradingPair.Symbol, argSymbol, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(queryTradingPair.BaseSymbol, argBaseSymbol, StringComparison.InvariantCultureIgnoreCase));

                return matchingTradingPair?.LotSize ?? DefaultLotSize;
            });

            var getPriceTick = new Func<string, string, decimal>((argSymbol, argBaseSymbol) =>
            {
                var matchingTradingPair = arbTradingPairs.SingleOrDefault(queryTradingPair => string.Equals(queryTradingPair.Symbol, argSymbol, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(queryTradingPair.BaseSymbol, argBaseSymbol, StringComparison.InvariantCultureIgnoreCase));

                return matchingTradingPair?.PriceTick ?? DefaultPriceTick;
            });

            var arbCommodity = arbCommodities.SingleOrDefault(queryCommodity => string.Equals(queryCommodity.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase));

            var compCommodity = compCommodities.SingleOrDefault(queryCommodity => string.Equals(queryCommodity.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase));

            decimal? altBaseVsBtcBestBidPrice = null;
            decimal? altBaseVsBtcBestAskPrice = null;
            if (!string.IsNullOrWhiteSpace(altBaseSymbol))
            {
                var altBaseVsBtcOrderBook = !string.IsNullOrWhiteSpace(altBaseSymbol)
                    ? GetOrderBookWithRetries(arbExchange, altBaseSymbol, "BTC", CachePolicy.AllowCache)
                    : null;

                altBaseVsBtcBestBidPrice = altBaseVsBtcOrderBook.BestBid()?.Price;
                altBaseVsBtcBestAskPrice = altBaseVsBtcOrderBook.BestAsk()?.Price;
            }

            // Removing COSS as a base symbol for now.
            // Need to get bi-directional valuations for now.
            var comparableBaseSymbols = new List<string> {
                "ETH",
                "BTC",
                // "COSS",
                "TUSD",
                //"USDT",
                //"QASH"
            };

            if (!string.IsNullOrWhiteSpace(altBaseSymbol))
            {
                comparableBaseSymbols.Add(altBaseSymbol);
            }

            var arbBaseSymbols =
                arbTradingPairs
                    .Where(item =>
                    string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                    && comparableBaseSymbols.Any(interestingBaseSymbol =>
                    string.Equals(interestingBaseSymbol, item.BaseSymbol, StringComparison.InvariantCultureIgnoreCase)))
                    .Select(item => item.BaseSymbol)
                        .ToList();

            if (arbBaseSymbols == null || !arbBaseSymbols.Any())
            {
                _log.Error($"{arbExchange} does not have any trading pairs for {symbol}");
                return;
            }

            CancelExistingOrders(arbExchange, symbol, arbBaseSymbols);

            var balanceSymbols = new List<string>();
            balanceSymbols.AddRange(arbBaseSymbols);
            if (!balanceSymbols.Any(queryBalanceSymbol => string.Equals(queryBalanceSymbol, symbol, StringComparison.InvariantCultureIgnoreCase)))
            {
                balanceSymbols.Add(symbol);
            }

            var isAltABaseSymbol = arbBaseSymbols.Any(queryArbBaseSymbol =>
                !string.IsNullOrWhiteSpace(altBaseSymbol)
                && string.Equals(queryArbBaseSymbol, altBaseSymbol, StringComparison.InvariantCultureIgnoreCase));

            var compBaseSymbols = compTradingPairs
                    .Where(item =>
                    string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                    && comparableBaseSymbols.Any(interestingBaseSymbol =>
                    string.Equals(interestingBaseSymbol, item.BaseSymbol, StringComparison.InvariantCultureIgnoreCase)))
                    .Select(item =>
                    {
                        return item.BaseSymbol;
                    })
                    .ToList();

            if (compBaseSymbols == null || !compBaseSymbols.Any())
            {
                _log.Warn($"{compExchange} does not have any trading pairs for {symbol}");
                return;
            }

            decimal? symbolUsdValue = null;
            decimal ethUsdValue = 0;
            decimal btcUsdValue = 0;
            decimal usdtUsdValue = 0;
            decimal tusdUsdValue = 1;

            var valuationTask = LongRunningTask.Run(() =>
            {
                ethUsdValue = _workflowClient.GetUsdValue("ETH", CachePolicy.AllowCache).Value;
                btcUsdValue = _workflowClient.GetUsdValue("BTC", CachePolicy.AllowCache).Value;
                usdtUsdValue = _workflowClient.GetUsdValue("USDT", CachePolicy.AllowCache).Value;
                symbolUsdValue = _workflowClient.GetUsdValue(symbol, CachePolicy.AllowCache);
            });

            List<(OrderBook OrderBook, string BaseSymbol)> arbOrderBooks = new List<(OrderBook OrderBook, string BaseSymbol)>();
            foreach (var queryBaseSymbol in arbBaseSymbols)
            {
                var orderBook = GetOrderBookWithRetries(arbExchange, symbol, queryBaseSymbol, CachePolicy.ForceRefresh);
                arbOrderBooks.Add((orderBook, queryBaseSymbol));
            }

            if (!((compCommodity.CanDeposit ?? false) || waiveCompDepositAndWithdrawalCheck)
                || !((compCommodity.CanWithdraw ?? false) || waiveCompDepositAndWithdrawalCheck))
            {
                _log.Info($"Must be able to do both deposits and withdrawals on the comp ({symbol} on {compExchange}) in order to use it for arbitrage.");
                return;
            }

            List<(OrderBook OrderBook, string BaseSymbol)> compOrderBooks = compBaseSymbols.Select(queryBaseSymbol =>
                (GetOrderBookWithRetries(compExchange, symbol, queryBaseSymbol, CachePolicy.ForceRefresh),
                queryBaseSymbol))
                .ToList();

            var balances = GetBalancesWithRetries(arbExchange, balanceSymbols, CachePolicy.ForceRefresh);
            var symbolBalance = balances.SingleOrDefault(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase));

            OrderBook arbBtcOrderBook = null;
            if (isAltABaseSymbol)
            {
                arbBtcOrderBook = GetOrderBookWithRetries(arbExchange, altBaseSymbol, "BTC", CachePolicy.ForceRefresh);
            }

            valuationTask.Wait();

            var valuations = new List<SymbolAndUsdValue>
            {
                new SymbolAndUsdValue { Symbol = "ETH", UsdValue = ethUsdValue },
                new SymbolAndUsdValue { Symbol = "BTC", UsdValue = btcUsdValue },
                new SymbolAndUsdValue { Symbol = "USDT", UsdValue = usdtUsdValue },
                new SymbolAndUsdValue { Symbol = "TUSD", UsdValue = tusdUsdValue }
            };

            bool didWeTryToPlaceAnyOrders = false;

            var arbAggregateOrderBook = GenerateAggregateOrderBook(
                arbOrderBooks.Select(queryArbOrderBook =>
                new OrderBookAndBaseSymbol
                {
                    OrderBook = queryArbOrderBook.OrderBook,
                    BaseSymbol = queryArbOrderBook.BaseSymbol
                }).ToList(),
                valuations, arbBtcOrderBook,
                altBaseSymbol);

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
                    new SymbolAndUsdValue { Symbol = "USDT", UsdValue = usdtUsdValue },
                    new SymbolAndUsdValue { Symbol = "TUSD", UsdValue = tusdUsdValue }
                },
                arbBtcOrderBook,
                altBaseSymbol);

            var arbAsks = arbAggregateOrderBook.Where(item => item.OrderType == OrderType.Ask).ToList();
            var arbBids = arbAggregateOrderBook.Where(item => item.OrderType == OrderType.Bid).ToList();

            var bestArbBid = arbBids.OrderByDescending(item => item.UsdPrice).FirstOrDefault();
            if (bestArbBid == null) { return; }
            var bestArbBidUsdPrice = bestArbBid.UsdPrice;
            var bestArbAsk = arbAsks.OrderBy(item => item.UsdPrice).FirstOrDefault();
            if (bestArbAsk == null) { return; }
            var bestArbAskUsdPrice = bestArbAsk.UsdPrice;

            var compAsks = compAggregateOrderBook.Where(item => item.OrderType == OrderType.Ask).ToList();
            var compBids = compAggregateOrderBook.Where(item => item.OrderType == OrderType.Bid).ToList();

            var bestCompBid = compBids.OrderByDescending(item => item.UsdPrice).FirstOrDefault();
            if (bestCompBid == null) { return; }
            var bestCompBidUsdPrice = bestCompBid.UsdPrice;
            var bestCompAsk = compAsks.OrderBy(item => item.UsdPrice).FirstOrDefault();
            if (bestCompAsk == null) { return; }
            var bestCompAskUsdPrice = bestCompAsk.UsdPrice;

            var symbolQuantityOwned = symbolBalance?.Total ?? 0;
            var symbolValueOwned = symbolQuantityOwned * (symbolUsdValue ?? 0);
            var isItOkToOwnMore = !maxUsdValueToOwn.HasValue || symbolValueOwned < maxUsdValueToOwn.Value;

            var baseSymbolsWithInfo = arbBaseSymbols.Select(queryBaseSymbol =>
                new BaseSymbolWithInfo
                {
                    BaseSymbol = queryBaseSymbol,
                    LotSize = getLotSize(symbol, queryBaseSymbol)
                }).ToList();

            if (isItOkToOwnMore &&
                ((arbCommodity.CanDeposit ?? false) || waiveArbDepositAndWithdrawalCheck) &&
                ((arbCommodity.CanWithdraw ?? false) || waiveArbDepositAndWithdrawalCheck) &&
                ((compCommodity.CanDeposit ?? false) || waiveCompDepositAndWithdrawalCheck) &&
                ((compCommodity.CanWithdraw ?? false) || waiveCompDepositAndWithdrawalCheck))
            {
                didWeTryToPlaceAnyOrders |= InstantBuy(arbExchange, symbol, arbAsks, baseSymbolsWithInfo, symbolUsdValue, bestCompBidUsdPrice);
            }

            if (((arbCommodity.CanDeposit ?? false) || waiveArbDepositAndWithdrawalCheck) &&
                ((arbCommodity.CanWithdraw ?? false) || waiveArbDepositAndWithdrawalCheck))
            {
                didWeTryToPlaceAnyOrders |= InstantSell(arbExchange, symbol, arbBids, baseSymbolsWithInfo, symbolUsdValue, bestCompAskUsdPrice, balances, arbTradingPairs);
            }

            if (((arbCommodity.CanDeposit ?? false) || waiveArbDepositAndWithdrawalCheck) &&
                ((arbCommodity.CanWithdraw ?? false) || waiveArbDepositAndWithdrawalCheck))
            {
                List<(OrderBook OrderBook, string BaseSymbol)> updatedArbOrderBookCombos =
                    didWeTryToPlaceAnyOrders
                    ? arbBaseSymbols.Select(queryBaseSymbol =>
                     (GetOrderBookWithRetries(arbExchange, symbol, queryBaseSymbol, CachePolicy.ForceRefresh),
                     queryBaseSymbol))
                    .ToList()
                    : arbOrderBooks;

                var updatedBalances = didWeTryToPlaceAnyOrders
                    ? GetBalancesWithRetries(arbExchange, balanceSymbols, CachePolicy.ForceRefresh)
                    : balances;

                didWeTryToPlaceAnyOrders = false;

                var updatedSymbolBalance = updatedBalances.ForSymbol(symbol);
                var updatedAvailableSymbolBalance = updatedSymbolBalance?.Available ?? 0;

                if ((arbCommodity.CanDeposit ?? waiveArbDepositAndWithdrawalCheck) && (arbCommodity.CanWithdraw ?? waiveArbDepositAndWithdrawalCheck))
                {
                    if (updatedAvailableSymbolBalance > 0)
                    {
                        var bestEffectiveCompBidPriceDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
                        {
                            { "ETH", (bestCompBidUsdPrice / ethUsdValue) },
                            { "BTC", (bestCompBidUsdPrice / btcUsdValue) },
                        };

                        if (altBaseVsBtcBestBidPrice.HasValue)
                        {
                            bestEffectiveCompBidPriceDictionary[altBaseSymbol] = bestCompBidUsdPrice / altBaseVsBtcBestBidPrice.Value / btcUsdValue;
                        }

                        var bestEffectiveCompAskPriceDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
                        {
                            { "ETH", (bestCompAskUsdPrice / ethUsdValue) },
                            { "BTC", (bestCompAskUsdPrice / btcUsdValue) },
                        };

                        if (altBaseVsBtcBestAskPrice.HasValue)
                        {
                            bestEffectiveCompAskPriceDictionary[altBaseSymbol] = bestCompBidUsdPrice / altBaseVsBtcBestAskPrice.Value / btcUsdValue;
                        }

                        var symbolsWithRoomForAnAskAndData = new List<(string BaseSymbol, decimal AskPrice)>();
                        foreach (var updatedCossOrderBookCombo in updatedArbOrderBookCombos)
                        {
                            var updatedArbOrderBook = updatedCossOrderBookCombo.OrderBook;
                            var baseSymbol = updatedCossOrderBookCombo.BaseSymbol;

                            var bestArbAskPrice = updatedArbOrderBook.BestAsk().Price;
                            var highestPriceWeCanWinWith = bestArbAskPrice - DefaultPriceTick;

                            decimal bestEffectiveCompAskSymbolPrice;
                            if (bestEffectiveCompAskPriceDictionary.ContainsKey(baseSymbol))
                            {
                                bestEffectiveCompAskSymbolPrice = bestEffectiveCompAskPriceDictionary[baseSymbol];
                            }
                            else if (isCompExchangeHighVolume && string.Equals(baseSymbol, "TUSD") && bestEffectiveCompAskPriceDictionary.ContainsKey("BTC"))
                            {
                                if (btcUsdValue <= 0) { throw new ApplicationException("BTC valuation failed."); }

                                var bestEffectiveCompAskBtcPrice = bestEffectiveCompAskPriceDictionary["BTC"];
                                bestEffectiveCompAskSymbolPrice = bestEffectiveCompAskBtcPrice * btcUsdValue;
                            }
                            else
                            {
                                _log.Error($"Failed to determine the comparison value of {symbol}-{baseSymbol}.");
                                continue;
                            }

                            // 15.0%
                            var idealAskPrice = MathUtil.ConstrainToMultipleOf(bestEffectiveCompAskSymbolPrice * (100.0m + effectiveIdealPercentDiff) / 100.0m, getPriceTick(symbol, baseSymbol));
                            decimal? ourAskPrice = null;
                            if (idealAskPrice < bestArbAskPrice)
                            {
                                ourAskPrice = idealAskPrice;
                            }
                            else
                            {
                                // ourAskPrice = bestArbAskPrice * 0.999m;
                                ourAskPrice = bestArbAskPrice - getPriceTick(symbol, baseSymbol);
                            }

                            var symbolDiff = ourAskPrice.Value - bestEffectiveCompAskSymbolPrice;
                            var symbolDiffRatio = symbolDiff / bestEffectiveCompAskSymbolPrice;

                            var isThereRoomForASymbolAsk = symbolDiffRatio >= (effectiveOpenAskMinPercentDiff / 100.0m);

                            if (isThereRoomForASymbolAsk)
                            {
                                if (!ourAskPrice.HasValue || ourAskPrice.Value <= 0)
                                {
                                    throw new ApplicationException("Something went horribly wrong.");
                                }

                                symbolsWithRoomForAnAskAndData.Add(
                                (
                                    baseSymbol,
                                    ourAskPrice.Value
                                ));
                            }
                        }

                        if (!symbolsWithRoomForAnAskAndData.Any())
                        {
                            _log.Info("ArbWorkflowUtil.AutoSymbol() - There is no room for an open-ask.");
                        }

                        for (var i = 0; i < symbolsWithRoomForAnAskAndData.Count; i++)
                        {
                            var symbolAndData = symbolsWithRoomForAnAskAndData[i];
                            var baseSymbol = symbolAndData.BaseSymbol;
                            var askPrice = symbolAndData.AskPrice;
                            var quantityToSell = updatedAvailableSymbolBalance
                                // / RoundUpErrorPrevention
                                / (symbolsWithRoomForAnAskAndData.Count);

                            quantityToSell = MathUtil.ConstrainToMultipleOf(quantityToSell, getLotSize(symbol, baseSymbol));

                            if (quantityToSell * askPrice < MinimumTradeDictionary[baseSymbol])
                            {
                                continue;
                            }

                            if (quantityToSell > 0)
                            {
                                if (!string.Equals(baseSymbol, altBaseSymbol, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    _log.Info($"About to place an open ask on {arbExchange} for {quantityToSell} {symbol} at {askPrice} {baseSymbol}.");
                                    try
                                    {
                                        didWeTryToPlaceAnyOrders = true;
                                        var sellLimitResult = _exchangeClient.SellLimit(arbExchange, symbol, baseSymbol, new QuantityAndPrice
                                        {
                                            Price = askPrice,
                                            Quantity = quantityToSell
                                        });

                                        if (sellLimitResult)
                                        {
                                            _log.Info($"Successfully placed a sell limit order for {quantityToSell} {symbol} at {askPrice} {baseSymbol}.");
                                        }
                                        else
                                        {
                                            _log.Error($"Failed to place a sell limit order for {quantityToSell} {symbol} at {askPrice} {baseSymbol}.");
                                        }
                                    }
                                    catch (Exception exception)
                                    {
                                        _log.Error($"Failed to place a sell limit order for {quantityToSell} {symbol} at {askPrice} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        _log.Info($"We don't have any {symbol} left on {arbExchange} to use for an open ask.");
                    }
                }

                // this could be made more efficient by keeping track of which order books
                // we tried to place orders on.
                if (didWeTryToPlaceAnyOrders)
                {
                    updatedArbOrderBookCombos = arbBaseSymbols.Select(queryBaseSymbol =>
                        (GetOrderBookWithRetries(arbExchange, symbol, queryBaseSymbol, CachePolicy.ForceRefresh),
                        queryBaseSymbol))
                        .ToList();

                    updatedBalances = GetBalancesWithRetries(arbExchange, balanceSymbols, CachePolicy.ForceRefresh);
                }

                updatedSymbolBalance = updatedBalances.ForSymbol(symbol);
                updatedAvailableSymbolBalance = updatedSymbolBalance?.Available ?? 0;

                var symbolValueDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
                {
                    { "ETH", ethUsdValue },
                    { "BTC", btcUsdValue },
                    { "TUSD", tusdUsdValue }
                };

                foreach (var arbOrderBookAndBaseSymbol in updatedArbOrderBookCombos)
                {
                    var baseSymbol = arbOrderBookAndBaseSymbol.BaseSymbol;
                    var baseSymbolValue = string.Equals(baseSymbol, altBaseSymbol, StringComparison.InvariantCultureIgnoreCase)
                        ? altBaseVsBtcBestBidPrice * btcUsdValue
                        : symbolValueDictionary[baseSymbol];

                    if (!baseSymbolValue.HasValue || baseSymbolValue <= 0)
                    {
                        _log.Warn($"Failed to determine base symbol value for {baseSymbol}");
                        continue;
                    }

                    var updatedArbBestSymbolBid = arbOrderBookAndBaseSymbol.OrderBook.BestBid();
                    var updatedArbBestSymbolBidPrice = updatedArbBestSymbolBid.Price;
                    // var updatedArbBestSymbolBidPriceAsUsd = updatedArbBestSymbolBidPrice * baseSymbolValue.Value;

                    var bestCompBidPriceAsSymbol = bestCompBidUsdPrice / baseSymbolValue.Value;

                    // try to bid 15% below the comp's best bid

                    var priceTick = getPriceTick(symbol, baseSymbol);
                    var potentialSymbolBidPrice = MathUtil.ConstrainToMultipleOf(bestCompBidPriceAsSymbol * (100.0m - effectiveIdealPercentDiff) / 100.0m, priceTick);
                    if (potentialSymbolBidPrice < updatedArbBestSymbolBidPrice)
                    {
                        potentialSymbolBidPrice = updatedArbBestSymbolBidPrice + priceTick;
                    }

                    // TODO: The decimal places limit needs to come from the exchange client.
                    if (string.Equals(arbExchange, IntegrationNameRes.Qryptos, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (string.Equals(symbol, "CAN", StringComparison.InvariantCultureIgnoreCase)
                            && string.Equals(baseSymbol, "QASH", StringComparison.InvariantCultureIgnoreCase))
                        {
                            potentialSymbolBidPrice = Math.Round(potentialSymbolBidPrice, 4);
                            if (potentialSymbolBidPrice == updatedArbBestSymbolBidPrice)
                            {
                                potentialSymbolBidPrice += 0.0001m;
                            }
                        }
                    }

                    var potentialSymbolBidAsUsd = potentialSymbolBidPrice * baseSymbolValue;

                    var compSymbolProfit = bestCompBidUsdPrice - potentialSymbolBidAsUsd;
                    var compSymbolProfitRatio = compSymbolProfit / potentialSymbolBidAsUsd;
                    var compProfitPercentage = 100.0m * compSymbolProfitRatio;
                    var valuationSymbolProfit = symbolUsdValue - potentialSymbolBidAsUsd;
                    var valuationSymbolProfitRatio = valuationSymbolProfit / potentialSymbolBidAsUsd;
                    var valuationSymbolProfitPercentage = 100.0m * valuationSymbolProfitRatio;

                    var updatedSymbolQuantityOwned = updatedSymbolBalance?.Total ?? 0;
                    var updatedSymbolValueOwned = updatedSymbolQuantityOwned * (symbolUsdValue ?? 0);
                    var isItStillOkToOwnMore = !maxUsdValueToOwn.HasValue || updatedSymbolValueOwned < maxUsdValueToOwn.Value;

                    if (isItStillOkToOwnMore && compProfitPercentage >= effectiveOpenBidMinPercentDiff && (!symbolUsdValue.HasValue || valuationSymbolProfitPercentage >= effectiveOpenBidMinPercentDiff))
                    {
                        var baseSymbolQuantity = effectiveOpenBidQuantityDictionary.ContainsKey(baseSymbol)
                            ? effectiveOpenBidQuantityDictionary[baseSymbol]
                            : 0.1m;

                        var baseSymbolBalance = updatedBalances.ForSymbol(baseSymbol);
                        var baseSymbolAvailable = baseSymbolBalance?.Available ?? 0;
                        if (string.Equals(baseSymbol, "QASH"))
                        {
                            // shouldn't have to do this, but the math doesn't match their error message.
                            // maybe if QASH is the fee token?
                            baseSymbolAvailable -= 1.0m;
                        }

                        if (baseSymbolAvailable <= 0) { continue; }
                        if (baseSymbolAvailable < baseSymbolQuantity)
                        {
                            baseSymbolQuantity = baseSymbolAvailable * RoundDownErrorPrevention;
                        }

                        var symbolQuantity = baseSymbolQuantity / potentialSymbolBidPrice;
                        symbolQuantity = MathUtil.ConstrainToMultipleOf(symbolQuantity, getLotSize(symbol, baseSymbol));

                        if (symbolQuantity > 0)
                        {
                            _log.Info($"About to place an open bid on {arbExchange} for {symbolQuantity} {symbol} at {potentialSymbolBidPrice} {baseSymbol}.");
                            try
                            {
                                var buyLimitResult = _exchangeClient.BuyLimit(arbExchange, symbol, baseSymbol, new QuantityAndPrice
                                {
                                    Quantity = symbolQuantity,
                                    Price = potentialSymbolBidPrice
                                });

                                if (buyLimitResult)
                                {
                                    _log.Info($"Successfully placed an open bid for {symbolQuantity} {symbol} at {potentialSymbolBidPrice} {baseSymbol}.");
                                }
                                else
                                {
                                    _log.Error($"Failed to buy {symbolQuantity} {symbol} at {potentialSymbolBidPrice} {baseSymbol}.");
                                }
                            }
                            catch (Exception exception)
                            {
                                _log.Error($"Failed to place an open bid on {arbExchange} for {symbolQuantity} {symbol} at {potentialSymbolBidPrice} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                                _log.Error(exception);
                            }
                        }
                    }
                    else
                    {
                        _log.Info($"There is no room for an open bid on {arbExchange} {symbol}-{baseSymbol}. Available Percent Diff: {(valuationSymbolProfitPercentage ?? 0).ToString("N4")}%; Required Percent Diff: {effectiveOpenBidMinPercentDiff.ToString("N4")}%");
                    }
                }
            }
        }
    }
}
