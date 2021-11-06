using arb_workflow_lib.Models;
using cache_lib.Models;
using config_client_lib;
using exchange_client_lib;
using log_lib;
using math_lib;
using object_extensions_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using task_lib;
using trade_constants;
using trade_model;
using workflow_client_lib;

namespace arb_workflow_lib
{
    public partial class ArbWorkflowUtil : IArbWorkflowUtil
    {
        private const decimal RoundUpErrorPrevention = 1.00001m;
        private const decimal RoundDownErrorPrevention = 1.0m / RoundUpErrorPrevention;

        // These values are from Coss.
        // They'll need to be configurable for each exchange.
        private const decimal MinimumTradeEth = 0.00251m;
        private const decimal MinimumTradeBtc = 0.00011m;
        private const decimal MinimumTradeCoss = 0.101m;

        // This value I made up. Who knows what the real value is????!!
        private const decimal MinimumTradeQash = 10;
        private const decimal MinimumTradeTusd = 1;

        private readonly IConfigClient _configClient;
        private readonly IExchangeClient _exchangeClient;
        private readonly IWorkflowClient _workflowClient;
        private readonly ILogRepo _log;

        public ArbWorkflowUtil(
            IConfigClient configClient,
            IExchangeClient exchangeClient,
            IWorkflowClient workflowClient,
            ILogRepo log)
        {
            _configClient = configClient;
            _exchangeClient = exchangeClient;
            _workflowClient = workflowClient;
            _log = log;

            // _exchangeClient.OverrideTimeout(TimeSpan.FromMinutes(2.5));
            _exchangeClient.OverrideTimeout(TimeSpan.FromMinutes(3.5));
        }

        // TODO: These really need to be configurable
        private const decimal InstantBuyMinimumPercentDiff = 4.0m;
        private const decimal OpenBidMinimumPercentDiff = 6.0m;
        private const decimal OpenAskMinimumPercentDiff = 3.5m;
        private const decimal InstantSellMinimumPercentDiff = 0;

        

        private void CancelExistingOrders(string arbExchange, string symbol, string baseSymbol)
        {
            CancelExistingOrders(arbExchange, symbol, new List<string> { baseSymbol });
        }

        private void CancelExistingOrders(string arbExchange, string symbol, List<string> arbBaseSymbols)
        {
            if (arbBaseSymbols == null || !arbBaseSymbols.Any())
            {
                _log.Info("No base symbols were provided.");
                return;
            }

            // Cancel any existing open orders.
            _log.Verbose("Begin cancel any open orders.");
            foreach (var baseSymbol in arbBaseSymbols)
            {
                try
                {
                    var existingArbOpenOrders = GetOpenOrdersWithRetries(arbExchange, symbol, baseSymbol, CachePolicy.ForceRefresh);
                    foreach (var openOrder in existingArbOpenOrders?.OpenOrders ?? new List<OpenOrder>())
                    {
                        CancelOrderWithRetries(arbExchange, openOrder.OrderId);
                    }
                }
                catch(Exception exception)
                {
                    _log.Error(exception);
                }
            }

            _log.Verbose("Done cancelling any open orders.");
        }

        private class BaseSymbolWithInfo
        {
            public string BaseSymbol { get; set; }
            public decimal? LotSize { get; set; }
        }

        /// <returns>True if we attempted to make any trades.</returns>
        private bool InstantBuy(
            string arbExchange,
            string symbol,
            List<AggregateOrderBookItem> arbAsks,
            List<BaseSymbolWithInfo> arbBaseSymbolsWithInfo,
            decimal? symbolUsdValue,
            decimal? bestCompBidUsdPrice
            )
        {
            bool didWeTryToPlaceAnyOrders = false;

            if (!bestCompBidUsdPrice.HasValue) { return didWeTryToPlaceAnyOrders; }

            // Instant buy
            var viableArbAsks = arbAsks.
                Where(cossAsk =>
                {
                    if (symbolUsdValue.HasValue && cossAsk.UsdPrice >= symbolUsdValue) { return false; }
                    var diff = bestCompBidUsdPrice.Value - cossAsk.UsdPrice;
                    var ratio = diff / cossAsk.UsdPrice;
                    var percentDiff = 100.0m * ratio;
                    return percentDiff >= InstantBuyMinimumPercentDiff;
                })
                .ToList();

            if (viableArbAsks.Any())
            {
                _log.Verbose("Found some asks that qualify for instant buy!");
                foreach (var baseSymbolWithInfo in arbBaseSymbolsWithInfo)
                {
                    var baseSymbol = baseSymbolWithInfo.BaseSymbol;
                    var lotSize = baseSymbolWithInfo.LotSize;

                    var viableCossAsksForBaseSymbol = viableArbAsks
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

                        if (lotSize.HasValue && lotSize.Value >= 1)
                        {
                            quantityToTakeForBaseSymbol = MathUtil.ConstrainToMultipleOf(quantityToTakeForBaseSymbol, lotSize.Value);
                        }

                        if (quantityToTakeForBaseSymbol > 0)
                        {
                            var limitOrderText = $"About to place a limit buy order for {quantityToTakeForBaseSymbol} {symbol} at {worstNativeAskPriceToTakeForBaseSymbol} {baseSymbol} on {arbExchange}.";
                            _log.Verbose(limitOrderText);
                            _log.Info(limitOrderText);

                            try
                            {
                                didWeTryToPlaceAnyOrders = true;
                                var buyLimitResult = _exchangeClient.BuyLimit(arbExchange, symbol, baseSymbol, new QuantityAndPrice
                                {
                                    Price = worstNativeAskPriceToTakeForBaseSymbol,
                                    Quantity = quantityToTakeForBaseSymbol
                                });

                                if (buyLimitResult)
                                {
                                    // _log.Verbose();
                                    var buyLimitSuccessText = $"Successfully placed a limit buy order for {quantityToTakeForBaseSymbol} {symbol} at {worstNativeAskPriceToTakeForBaseSymbol} {baseSymbol} on {arbExchange}.";
                                    _log.Info(buyLimitSuccessText);
                                    _log.Verbose(buyLimitSuccessText);
                                }
                                else
                                {
                                    var buyLimitFailureText = $"Failed to buy {quantityToTakeForBaseSymbol} {symbol} at {worstNativeAskPriceToTakeForBaseSymbol} {baseSymbol} on {arbExchange}.";
                                    _log.Error(buyLimitFailureText);
                                    _log.Verbose(buyLimitFailureText);
                                }
                            }
                            catch(Exception exception)
                            {
                                _log.Error($"Failed to buy {quantityToTakeForBaseSymbol} {symbol} at {worstNativeAskPriceToTakeForBaseSymbol} {baseSymbol} on {arbExchange}.{Environment.NewLine}{exception.Message}");
                                _log.Error(exception);
                            }
                        }
                    }
                }
            }
            else
            {
                _log.Verbose("There are no asks that qualify for instant buy");
            }

            return didWeTryToPlaceAnyOrders;
        }

        public void RollingBinanceTusdPurchase()
        {
            const decimal TargetAmount = 1500.0m;

            const string Exchange = IntegrationNameRes.Binance;
            const string Symbol = "TUSD";

            var baseSymbols = new List<string> { "BTC", "ETH",
                // "USDT"
            };

            var bidIncDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "BTC", 0.00000001m },
                { "ETH", 0.00000001m },
                { "BNB",  0.0001m },
                { "USDT", 0.0001m }
            };

            const int MaxIterations = 100;
            for (var i = 0; i < MaxIterations; i++)
            {
                try
                {
                    var balances = _exchangeClient.GetBalances(Exchange, CachePolicy.ForceRefresh);
                    var targetSymbolBalance = balances.GetHoldingForSymbol(Symbol);
                    foreach (var baseSymbol in baseSymbols)
                    {
                        var openOrders = _exchangeClient.GetOpenOrdersForTradingPairV2(Exchange, Symbol, baseSymbol, CachePolicy.ForceRefresh);
                        var openBids = (openOrders?.OpenOrders ?? new List<OpenOrder>()).Where(item => item.OrderType == OrderType.Bid).ToList();

                        if (targetSymbolBalance.Total >= TargetAmount)
                        {
                            foreach (var queryOpenBid in openBids)
                            {
                                _exchangeClient.CancelOrder(Exchange, queryOpenBid.OrderId);
                            }

                            continue;
                        }

                        if (openBids.Count > 1)
                        {
                            foreach (var queryOpenBid in openBids)
                            {
                                _exchangeClient.CancelOrder(Exchange, queryOpenBid.OrderId);
                            }
                        }

                        var openBid = openBids.Count == 1 ? openBids.Single() : null;

                        var orderBook = _exchangeClient.GetOrderBook(Exchange, Symbol, baseSymbol, CachePolicy.ForceRefresh);
                        if (orderBook == null) { continue; }

                        var bestBidPrice = orderBook.BestBid().Price;
                        if (openBid != null)
                        {
                            if (openBid.Price >= bestBidPrice) { continue; }
                            else { _exchangeClient.CancelOrder(Exchange, openBid.OrderId); }
                        }

                        // TODO: should include the amount recouped from any canceled orders.
                        var availableBalanceForSymbol = balances.GetAvailableForSymbol(baseSymbol);

                        var bidToPlacePrice = bestBidPrice + bidIncDictionary[baseSymbol];
                        var quantity = 0.95m * availableBalanceForSymbol / bidToPlacePrice;
                        if (quantity > 100) { quantity = 100; }
                        if (quantity <= 10) { continue; }

                        _log.Info($"About to place bid on {Exchange} for {quantity} {Symbol} at {bidToPlacePrice} {baseSymbol}.");
                        var orderResult = _exchangeClient.BuyLimit(Exchange, Symbol, baseSymbol, new QuantityAndPrice
                        {
                            Quantity = quantity,
                            Price = bidToPlacePrice
                        });

                        if (orderResult)
                        {
                            _log.Info($"Successfully place a bid on {Exchange} for {quantity} {Symbol} at {bidToPlacePrice} {baseSymbol}.");
                        }
                        else
                        {
                            _log.Info($"Failed to place a bid on {Exchange} for {quantity} {Symbol} at {bidToPlacePrice} {baseSymbol}.");
                        }
                    }

                    if (targetSymbolBalance.Total >= TargetAmount)
                    {
                        break;
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                }
            }

            foreach (var baseSymbol in baseSymbols)
            {
                var openOrders = _exchangeClient.GetOpenOrdersForTradingPairV2(Exchange, Symbol, baseSymbol, CachePolicy.ForceRefresh);
                if (openOrders?.OpenOrders != null && openOrders.OpenOrders.Any())
                {
                    foreach (var openOrder in openOrders.OpenOrders)
                    {
                        _exchangeClient.CancelOrder(Exchange, openOrder.OrderId);
                    }
                }
            }
        }

        public void KucoinUsdc()
        {
            // price tick eth
            // 0.0000001

            // price tick btc
            // 0.00000001

            const decimal PriceTickEth = 0.0000001m;
            const decimal PriceTickBtc = 0.00000001m;
            const decimal MinProfitPercent = 1.0m;
            const decimal MaxProfitPercent = 20.0m; // If the profit is this high on a stable coin, something has gone horribly wrong.

            const string Exchange = IntegrationNameRes.KuCoin;
            const string AcquisitionSymbol = "USDC";

            const decimal QuantityToBuy = 10.0m;
            const decimal MinQuantityToSell = 1.0m;

            OrderBook binanceEthBtcOrderBook = null;
            var binanceTask = LongRunningTask.Run(() =>
            {
                binanceEthBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, "ETH", "BTC", CachePolicy.ForceRefresh);
            });

            CancelExistingOrders(Exchange, AcquisitionSymbol, new List<string> { "ETH", "BTC" });

            var balances = GetBalancesWithRetries(Exchange, new List<string> { AcquisitionSymbol, "ETH", "BTC" }, CachePolicy.ForceRefresh);
            var usdcBalance = balances.SingleOrDefault(item => string.Equals(item.Symbol, AcquisitionSymbol, StringComparison.InvariantCultureIgnoreCase));
            var ethBalance = balances.SingleOrDefault(item => string.Equals(item.Symbol, "ETH", StringComparison.InvariantCultureIgnoreCase));
            var btcBalance = balances.SingleOrDefault(item => string.Equals(item.Symbol, "BTC", StringComparison.InvariantCultureIgnoreCase));

            var kucoinUsdcEthOrderBook = GetOrderBookWithRetries(Exchange, AcquisitionSymbol, "ETH", CachePolicy.ForceRefresh);
            var kucoinUsdcBtcOrderBook = GetOrderBookWithRetries(Exchange, AcquisitionSymbol, "BTC", CachePolicy.ForceRefresh);

            binanceTask.Wait();

            var binanceEthBtcBestAskPrice = binanceEthBtcOrderBook.BestAsk().Price;
            if (binanceEthBtcBestAskPrice <= 0) { throw new ApplicationException("Binance ETH-BTC best Ask price should be > 0."); }

            var binanceEthBtcBestBidPrice = binanceEthBtcOrderBook.BestBid().Price;
            if (binanceEthBtcBestBidPrice <= 0) { throw new ApplicationException("Binance ETH-BTC best Bid price should be > 0."); }
            var ethBtcRatio = new List<decimal> { binanceEthBtcBestAskPrice, binanceEthBtcBestBidPrice }.Average();

            var kucoinUsdcBtcBestBidPrice = kucoinUsdcBtcOrderBook.BestBid().Price;
            if (kucoinUsdcBtcBestBidPrice <= 0) { throw new ApplicationException($"{Exchange} {AcquisitionSymbol}-BTC best Bid price should be > 0."); }

            var kucoinUsdcEthBestBidPrice = kucoinUsdcEthOrderBook.BestBid().Price;            
            if (kucoinUsdcEthBestBidPrice <= 0) { throw new ApplicationException($"{Exchange} {AcquisitionSymbol}-BTC best Bid price should be > 0."); }

            var kucoinUsdcBtcBestAskPrice = kucoinUsdcBtcOrderBook.BestAsk().Price;
            if (kucoinUsdcBtcBestAskPrice <= 0) { throw new ApplicationException($"{Exchange} {AcquisitionSymbol}-ETH best Ask price should be > 0."); }

            var kucoinUsdcEthBestAskPrice = kucoinUsdcEthOrderBook.BestAsk().Price;
            if (kucoinUsdcEthBestAskPrice <= 0) { throw new ApplicationException($"{Exchange} {AcquisitionSymbol}-ETH best Ask price should be > 0."); }

            var kucoinUsdcEthBestBidPriceAsBtc = kucoinUsdcEthBestBidPrice * ethBtcRatio;
            var kucoinUsdcEthBestAskPriceAsBtc = kucoinUsdcEthBestAskPrice * ethBtcRatio;

            var kucoinUsdcBtcValuation = new List<decimal> { kucoinUsdcBtcBestBidPrice, kucoinUsdcBtcBestAskPrice, kucoinUsdcEthBestBidPriceAsBtc, kucoinUsdcEthBestAskPriceAsBtc }
                .Average();

            var kucoinUsdcEthValuation = kucoinUsdcBtcValuation / ethBtcRatio;

            if (kucoinUsdcEthBestBidPriceAsBtc < kucoinUsdcBtcBestBidPrice)
            {
                const string BaseSymbol = "ETH";
                var quantity = QuantityToBuy;

                var price = kucoinUsdcEthBestBidPrice + PriceTickEth;
                var diff = kucoinUsdcEthValuation - price;
                var ratio = diff / kucoinUsdcEthValuation;
                var profitPercent = 100.0m * ratio;

                if (profitPercent >= MinProfitPercent && profitPercent <= MaxProfitPercent)
                {
                    _log.Info($"The potential profit for {Exchange} {AcquisitionSymbol}-{BaseSymbol} is {profitPercent}%. That is within of the threshold of {MinProfitPercent}%-{MaxProfitPercent}%.");

                    try
                    {
                        _log.Info($"About to place a buy limit order on {Exchange} for {quantity} {AcquisitionSymbol} at {price} {BaseSymbol}.");
                        var orderResult = _exchangeClient.BuyLimit(Exchange, AcquisitionSymbol, BaseSymbol, new QuantityAndPrice
                        {
                            Price = price,
                            Quantity = quantity
                        });

                        if (orderResult)
                        {
                            _log.Info($"Successfully placed a buy limit order on {Exchange} for {quantity} {AcquisitionSymbol} at {price} {BaseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place a buy limit order on {Exchange} for {quantity} {AcquisitionSymbol} at {price} {BaseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place a buy limit order on {Exchange} for {quantity} {AcquisitionSymbol} at {price} {BaseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }                
                else
                {
                    _log.Info($"The potential profit for {Exchange} {AcquisitionSymbol}-{BaseSymbol} is {profitPercent}%. That is outside of the threshold of {MinProfitPercent}%-{MaxProfitPercent}%.");
                }
            }
            else
            {
                const string BaseSymbol = "BTC";
                var quantity = QuantityToBuy;

                var price = kucoinUsdcBtcBestBidPrice + PriceTickBtc;
                var diff = kucoinUsdcBtcValuation - price;
                var ratio = diff / kucoinUsdcBtcValuation;
                var profitPercent = 100.0m * ratio;

                if (profitPercent >= MinProfitPercent && profitPercent <= MaxProfitPercent)
                {
                    _log.Info($"The potential profit for {Exchange} {AcquisitionSymbol}-{BaseSymbol} is {profitPercent}%. That is within of the threshold of {MinProfitPercent}%-{MaxProfitPercent}%.");
                    try
                    {
                        _log.Info($"About to place a buy limit order on {Exchange} for {quantity} {AcquisitionSymbol} at {price} {BaseSymbol}.");
                        var orderResult = _exchangeClient.BuyLimit(Exchange, AcquisitionSymbol, BaseSymbol, new QuantityAndPrice
                        {
                            Price = price,
                            Quantity = quantity
                        });

                        if (orderResult)
                        {
                            _log.Info($"Successfully placed a buy limit order on {Exchange} for {quantity} {AcquisitionSymbol} at {price} {BaseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place a buy limit order on {Exchange} for {quantity} {AcquisitionSymbol} at {price} {BaseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place a buy limit order on {Exchange} for {quantity} {AcquisitionSymbol} at {price} {BaseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
                else
                {
                    _log.Info($"The potential profit for {Exchange} {AcquisitionSymbol}-{BaseSymbol} is {profitPercent}%. That is outside of the threshold of {MinProfitPercent}%-{MaxProfitPercent}%.");
                }
            }

            var usdcQuantityAvailable = usdcBalance != null ? usdcBalance.Available : 0;
            if (usdcQuantityAvailable >= MinQuantityToSell)
            {
                if (kucoinUsdcEthBestAskPriceAsBtc >= kucoinUsdcBtcBestAskPrice)
                {
                    const string BaseSymbol = "ETH";
                }
                else
                {
                    const string BaseSymbol = "BTC";
                }
            }
        }

        /// <returns>True if we attempted to make any trades.</returns>
        private bool InstantSell(
            string arbExchange,
            string symbol,
            List<AggregateOrderBookItem> arbBids,
            List<BaseSymbolWithInfo> arbBaseSymbolsWithInfo,
            decimal? symbolUsdValue,
            decimal bestCompAskUsdPrice,
            List<BalanceWithAsOf> balances,
            List<TradingPair> tradingPairs)
        {
            var getMatchingTradingPair = new Func<string, string, TradingPair>((querySymbol, queryBaseSymbol) => tradingPairs.Single(queryTradingPair =>
                string.Equals(queryTradingPair.Symbol, querySymbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(queryTradingPair.BaseSymbol, queryBaseSymbol, StringComparison.InvariantCultureIgnoreCase)
            ));

            var getLotSize = new Func<string, string, decimal>((querySymbol, queryBaseSymbol) =>
            {
                var matchingTradingPair = getMatchingTradingPair(querySymbol, queryBaseSymbol);
                if (!matchingTradingPair.LotSize.HasValue) { throw new ApplicationException($"Trading pair {querySymbol}-{queryBaseSymbol} must have a lot size."); }
                if (matchingTradingPair.LotSize.Value <= 0) { throw new ApplicationException($"Trading pair {querySymbol}-{queryBaseSymbol}'s lot size must be > 0."); }

                return matchingTradingPair.LotSize.Value;
            });

            var getPriceTick = new Func<string, string, decimal>((querySymbol, queryBaseSymbol) =>
            {
                var matchingTradingPair = getMatchingTradingPair(querySymbol, queryBaseSymbol);
                if (!matchingTradingPair.PriceTick.HasValue) { throw new ApplicationException($"Trading pair {querySymbol}-{queryBaseSymbol} must have a price tick."); }
                if (matchingTradingPair.PriceTick.Value <= 0) { throw new ApplicationException($"Trading pair {querySymbol}-{queryBaseSymbol}'s price tick must be > 0."); }

                return matchingTradingPair.PriceTick.Value;
            });

            bool didWeTryToPlaceAnyOrders = false;

            // determine the viable arb bids.
            var viableArbBids = new List<AggregateOrderBookItem>();
            foreach (var cossBid in arbBids)
            {
                var diff = cossBid.UsdPrice - bestCompAskUsdPrice;
                var ratio = diff / bestCompAskUsdPrice;
                var percentDiff = 100.0m * ratio;
                if (percentDiff >= InstantSellMinimumPercentDiff)
                {
                    if (symbolUsdValue.HasValue && cossBid.UsdPrice < symbolUsdValue)
                    {
                        _log.Verbose("Would have taken this bid, but it's below the valuation price.");
                        continue;
                    }

                    viableArbBids.Add(cossBid);
                }
            }

            var symbolBalance = balances.ForSymbol(symbol);
            var symbolBalanceAvailable = symbolBalance?.Available ?? 0;
            var baseSymbolsThatWeSoldAgainst = new List<string>();
            if (viableArbBids.Any())
            {
                foreach (var baseSymbolWithInfo in arbBaseSymbolsWithInfo)
                {
                    var baseSymbol = baseSymbolWithInfo.BaseSymbol;

                    var viableArbBidsForBaseSymbol = viableArbBids.Where(item => string.Equals(item.BaseSymbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase)).ToList();

                    if (viableArbBidsForBaseSymbol.Any())
                    {
                        var worstNativeBidPriceToTakeForBaseSymbol = viableArbBidsForBaseSymbol.OrderBy(item => item.NativePrice).First().NativePrice;
                        var priceToSellAt = MathUtil.ConstrainToMultipleOf(worstNativeBidPriceToTakeForBaseSymbol / RoundUpErrorPrevention, getPriceTick(symbol, baseSymbol));
                        var quantityToSell = MathUtil.ConstrainToMultipleOf(viableArbBidsForBaseSymbol.Sum(item => item.Quantity) * RoundUpErrorPrevention, getLotSize(symbol, baseSymbol));

                        var baseSymbolTradeQuantity = priceToSellAt * quantityToSell;
                        var minimumTradeForBaseSymbol = GetMinimumTradeForBaseSymbol(baseSymbol) ?? 0;

                        if (baseSymbolTradeQuantity < minimumTradeForBaseSymbol)
                        {
                            quantityToSell = minimumTradeForBaseSymbol / worstNativeBidPriceToTakeForBaseSymbol * RoundUpErrorPrevention;
                            if (quantityToSell > symbolBalanceAvailable)
                            {
                                quantityToSell = 0;
                            }
                        }
                        else if (quantityToSell > symbolBalanceAvailable)
                        {
                            quantityToSell = symbolBalanceAvailable;
                            var updatedBaseSymbolTradeQuantity = quantityToSell * priceToSellAt;
                            if (updatedBaseSymbolTradeQuantity < minimumTradeForBaseSymbol)
                            {
                                quantityToSell = 0;
                            }
                        }

                        quantityToSell = MathUtil.ConstrainToMultipleOf(quantityToSell, getLotSize(symbol, baseSymbol));

                        if (quantityToSell > 0)
                        {
                            _log.Info($"About to place a sell limit order for {quantityToSell} {symbol} at {worstNativeBidPriceToTakeForBaseSymbol} {baseSymbol}");
                            try
                            {
                                baseSymbolsThatWeSoldAgainst.Add(baseSymbol);

                                didWeTryToPlaceAnyOrders = true;
                                var placeOrderResult = _exchangeClient.SellLimit(arbExchange, symbol, baseSymbol, new QuantityAndPrice
                                {
                                    Quantity = quantityToSell,
                                    Price = priceToSellAt
                                });

                                if (placeOrderResult)
                                {
                                    _log.Info($"Successfully placed a sell limit order for {quantityToSell} {symbol} at {worstNativeBidPriceToTakeForBaseSymbol} {baseSymbol}");
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

                            var openOrdersAfterSales = GetOpenOrdersWithRetries(arbExchange, symbol, baseSymbol, CachePolicy.ForceRefresh);
                            foreach (var openOrder in openOrdersAfterSales?.OpenOrders ?? new List<OpenOrder>())
                            {
                                try
                                {
                                    _exchangeClient.CancelOrder(arbExchange, openOrder.OrderId);
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
            else
            {
                _log.Verbose("There are no bids that qualify for instant sell");
            }

            return didWeTryToPlaceAnyOrders;
        }

        private List<BalanceWithAsOf> GetBalancesWithRetries(
            string exchange,
            List<string> symbols,
            CachePolicy cachePolicy)
        {
            if (string.Equals(exchange, IntegrationNameRes.Qryptos, StringComparison.InvariantCultureIgnoreCase))
            {
                var balancesWithAsOf = new List<BalanceWithAsOf>();
                foreach (var symbol in symbols ?? new List<string>())
                {
                    var balance = AttemptWithRetries(() => _exchangeClient.GetBalance(exchange, symbol, cachePolicy));
                    balancesWithAsOf.Add(new BalanceWithAsOf
                    {
                        AsOfUtc = null,
                        Available = balance?.Available,
                        InOrders = balance?.InOrders,
                        Total = balance?.Total,
                        Symbol = symbol,
                        AdditionalBalanceItems = balance?.AdditionalHoldings
                    });
                }

                return balancesWithAsOf;
            }

            return AttemptWithRetries(() => _exchangeClient.GetBalances(exchange, symbols, cachePolicy));
        }

        private Holding GetBalanceWithRetries(string exchange, string symbol, CachePolicy cachePolicy)
        {
            return AttemptWithRetries(() => _exchangeClient.GetBalance(exchange, symbol, cachePolicy));
        }

        private OrderBook GetOrderBookWithRetries(string exchange, string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            _log.Verbose($"Getting {exchange} order book for {symbol}-{baseSymbol} with cache policy {cachePolicy}.");
            var result = AttemptWithRetries(() => _exchangeClient.GetOrderBook(exchange, symbol, baseSymbol, cachePolicy));
            _log.Verbose($"  Done getting {exchange} order book for {symbol}-{baseSymbol} with cache policy {cachePolicy}.");

            return result;
        }

        private OpenOrdersWithAsOf GetOpenOrdersWithRetries(
            string exchange, string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            _log.Verbose($"Getting {exchange} open orders for {symbol}-{baseSymbol} with cache policy {cachePolicy}.");
            var result = AttemptWithRetries(() =>
            {
                var innerResult = _exchangeClient.GetOpenOrdersForTradingPairV2(exchange, symbol, baseSymbol, cachePolicy);
                return innerResult;
            });

            _log.Verbose($"  Done getting {exchange} open orders for {symbol}-{baseSymbol} with cache policy {cachePolicy}.");

            return result;
        }

        private OpenOrdersWithAsOf GetOpenOrdersForTradingPairV2WithRetries(string exchange, string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            return AttemptWithRetries(() => _exchangeClient.GetOpenOrdersForTradingPairV2(exchange, symbol, baseSymbol, cachePolicy));
        }

        private void CancelOrderWithRetries(string exchange, string orderId)
        {
            AttemptWithRetries(() => _exchangeClient.CancelOrder(exchange, orderId));
        }

        private List<TradingPair> GetTradingPairsWithRetries(string exchange, CachePolicy cachePolicy)
        {
            return AttemptWithRetries(() => _exchangeClient.GetTradingPairs(exchange, cachePolicy));
        }

        private List<CommodityForExchange> GetCommoditiesWithRetries(string exchange, CachePolicy cachePolicy)
        {
            return _exchangeClient.GetCommoditiesForExchange(exchange, cachePolicy);
        }

        private void AttemptWithRetries(Action method)
        {
            AttemptWithRetries(new Func<int>(() =>
            {
                method();
                return 1;
            }));
        }

        private T AttemptWithRetries<T>(Func<T> method)
        {
            Exception lastException = null;

            const int MaxRetries = 5;
            for (var i = 0; i < MaxRetries; i++)
            {
                try
                {
                    if (i != 0)
                    {
                        var secondsToSleep = 1.75d * i;
                        Thread.Sleep((int)secondsToSleep);
                    }
                    return method();
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                    lastException = exception;
                }
            }

            throw lastException;
        }

        private static decimal? GetMinimumTradeForBaseSymbol(string baseSymbol)
        {
            return MinimumTradeDictionary.ContainsKey(baseSymbol)
                ? MinimumTradeDictionary[baseSymbol]
                : (decimal?)null;
        }

        private static Dictionary<string, decimal> MinimumTradeDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "ETH", MinimumTradeEth },
            { "BTC", MinimumTradeBtc },
            { "COSS", MinimumTradeCoss },
            { "QASH", MinimumTradeQash },
            { "TUSD", MinimumTradeTusd },
        };

        private List<AggregateOrderBookItem> GenerateAggregateOrderBook(
            List<OrderBookAndBaseSymbol> orderBooks,
            List<SymbolAndUsdValue> valuations,
            OrderBook altVsBtcOrderBook = null,
            string altBaseSymbol = null)
        {
            var effectiveAltBaseSymbol = !string.IsNullOrWhiteSpace(altBaseSymbol)
                ? altBaseSymbol :
                "COSS";

            var symbolOrderBooks = new List<(string BaseSymbol, List<Order> Orders, OrderType OrderType)>();
            foreach (var orderBookAndBaseSymbol in orderBooks)
            {
                symbolOrderBooks.Add((orderBookAndBaseSymbol.BaseSymbol, orderBookAndBaseSymbol.OrderBook.Asks, OrderType.Ask));
                symbolOrderBooks.Add((orderBookAndBaseSymbol.BaseSymbol, orderBookAndBaseSymbol.OrderBook.Bids, OrderType.Bid));
            }

            var btcUsdPrice = valuations.Single(queryValuation => string.Equals(queryValuation.Symbol, "BTC", StringComparison.InvariantCultureIgnoreCase)).UsdValue;
            var ethUsdPrice = valuations.Single(queryValuation => string.Equals(queryValuation.Symbol, "ETH", StringComparison.InvariantCultureIgnoreCase)).UsdValue;

            var aggregateOrderBook = new List<AggregateOrderBookItem>();
            foreach (var symbolOrderBook in symbolOrderBooks)
            {
                if (string.Equals(symbolOrderBook.BaseSymbol, effectiveAltBaseSymbol, StringComparison.InvariantCultureIgnoreCase))
                {
                    // TODO: really should be taking into account both the COSS-ETH and COSS-BTC order books
                    if (altVsBtcOrderBook != null)
                    {
                        foreach (var order in symbolOrderBook.Orders ?? new List<Order>())
                        {
                            if (symbolOrderBook.OrderType == OrderType.Bid)
                            {
                                var cossVsBtcBestBid = altVsBtcOrderBook.BestBid();
                                var cossVsBtcBestBidPrice = cossVsBtcBestBid.Price;

                                var btcEquivalentPrice = order.Price * cossVsBtcBestBidPrice;
                                var usdEquivalentPrice = btcEquivalentPrice * btcUsdPrice;

                                var aggy = new AggregateOrderBookItem
                                {
                                    BaseSymbol = symbolOrderBook.BaseSymbol,
                                    NativePrice = order.Price,
                                    Quantity = order.Quantity,
                                    OrderType = symbolOrderBook.OrderType,
                                    UsdPrice = usdEquivalentPrice
                                };

                                aggregateOrderBook.Add(aggy);
                            }
                            else if (symbolOrderBook.OrderType == OrderType.Ask)
                            {
                                var cossVsBtcBestAsk = altVsBtcOrderBook.BestAsk();
                                var cossVsBtcBestAskPrice = cossVsBtcBestAsk.Price;
                                var btcEquivalentPrice = order.Price * cossVsBtcBestAskPrice;
                                var usdEquivalentPrice = btcEquivalentPrice * btcUsdPrice;

                                var aggy = new AggregateOrderBookItem
                                {
                                    BaseSymbol = symbolOrderBook.BaseSymbol,
                                    NativePrice = order.Price,
                                    Quantity = order.Quantity,
                                    OrderType = symbolOrderBook.OrderType,
                                    UsdPrice = usdEquivalentPrice
                                };

                                aggregateOrderBook.Add(aggy);
                            }
                        }
                    }
                }
                else
                {
                    var baseSymbolUsdValue = valuations.Single(queryValution =>
                        string.Equals(queryValution.Symbol, symbolOrderBook.BaseSymbol, StringComparison.InvariantCultureIgnoreCase)
                    );

                    aggregateOrderBook.AddRange(
                    symbolOrderBook.Orders.Select(queryOrder =>
                    {
                        return new AggregateOrderBookItem
                        {
                            BaseSymbol = symbolOrderBook.BaseSymbol,
                            NativePrice = queryOrder.Price,
                            Quantity = queryOrder.Quantity,
                            OrderType = symbolOrderBook.OrderType,
                            UsdPrice = queryOrder.Price * baseSymbolUsdValue.UsdValue
                        };
                    }));
                }
            }

            aggregateOrderBook = aggregateOrderBook != null
                ? aggregateOrderBook.OrderBy(item => item.UsdPrice).ToList()
                : null;

            return aggregateOrderBook;
        }
        
        // does no do comparisons. just does a +1 bid.
        public void AutoBuy(string exchange, string symbol, string baseSymbol, decimal? maxToAcquire = null)
        {
            _log.Info($"Beginning auto-buy on {exchange} for {symbol}-{baseSymbol}.");

            var tradingPairs = _exchangeClient.GetTradingPairs(exchange, CachePolicy.AllowCache);
            var tradingPair = tradingPairs.Single(item => string.Equals(symbol, item.Symbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(baseSymbol, item.BaseSymbol, StringComparison.InvariantCultureIgnoreCase));

            if (!tradingPair.PriceTick.HasValue) { throw new ApplicationException($"{exchange}'s {symbol}-{baseSymbol} pair does not have a price tick configured."); }
            if (tradingPair.PriceTick.Value <= 0) { throw new ApplicationException($"{exchange}'s {symbol}-{baseSymbol} price tick must be > 0 to do an auto buy."); }

            var priceTick = tradingPair.PriceTick.Value;

            if (!tradingPair.LotSize.HasValue) { throw new ApplicationException($"{exchange}'s {symbol}-{baseSymbol} pair does not have a lot size configured."); }
            if (tradingPair.LotSize.Value <= 0) { throw new ApplicationException($"{exchange}'s {symbol}-{baseSymbol} lot size must be > 0 to do an auto buy."); }
            var lotSize = tradingPair.LotSize.Value;

            var holdingInfo = _exchangeClient.GetBalances(exchange, CachePolicy.ForceRefresh);
            var baseSymbolHolding = holdingInfo.GetHoldingForSymbol(baseSymbol);
            var baseSymbolAvailable = baseSymbolHolding?.Available ?? 0;
            var symbolHolding = holdingInfo.GetHoldingForSymbol(symbol);
            var symbolTotal = symbolHolding?.Total ?? 0;

            var openOrdersResult = _exchangeClient.GetOpenOrdersForTradingPairV2(exchange, symbol, baseSymbol, CachePolicy.ForceRefresh);
            var openOrders = openOrdersResult.OpenOrders ?? new List<OpenOrder>();
            var openBids = openOrders.Where(item => item.OrderType == OrderType.Bid).ToList();

            if (maxToAcquire.HasValue && symbolTotal >= maxToAcquire * 1.01m)
            {
                Console.WriteLine($"We already have enough {symbol}.");

                foreach(var openBid in openBids)
                {
                    _exchangeClient.CancelOrder(exchange, openBid);
                }

                return;
            }

            OpenOrder existingBid = null;

            OrderBook orderBook = null;
            if (openBids.Count >= 2)
            {
                foreach(var openBid in openBids)
                {
                    _exchangeClient.CancelOrder(exchange, openBid.OrderId);
                }
            }
            else if(openBids.Count == 1)
            {
                existingBid = openBids.Single();
                orderBook = _exchangeClient.GetOrderBook(exchange, symbol, baseSymbol, CachePolicy.ForceRefresh);
                var matchingOrder = orderBook.Bids.FirstOrDefault(item => item.Price == existingBid.Price && item.Quantity == existingBid.Quantity);
                if (matchingOrder != null)
                {
                    orderBook.Bids.Remove(matchingOrder);
                    baseSymbolAvailable += existingBid.Quantity * existingBid.Price;
                }
            }

            if (orderBook == null)
            {
                orderBook = _exchangeClient.GetOrderBook(exchange, symbol, baseSymbol, CachePolicy.ForceRefresh);
            }

            var bestBidPrice = orderBook.BestBid().Price;

            var upTickBidPrice = bestBidPrice + priceTick;
            var maxPossible = baseSymbolAvailable / upTickBidPrice / 1.01m;

            var remainingToAcquire = maxToAcquire.HasValue ? maxToAcquire.Value - symbolTotal : (decimal?)null;

            var unconstrainedQuantity = remainingToAcquire.HasValue && remainingToAcquire.Value <= maxPossible
                ? remainingToAcquire.Value
                : maxPossible;
            
            var quantity = MathUtil.ConstrainToMultipleOf(unconstrainedQuantity, lotSize);
            var price = upTickBidPrice;

            _log.Info($"About to place a bid on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
            try
            {
                var orderResult = _exchangeClient.BuyLimitV2(exchange, symbol, baseSymbol, new QuantityAndPrice
                {
                    Quantity = quantity,
                    Price = price
                });

                if (orderResult.WasSuccessful)
                {
                    _log.Info($"Successfully placed a bid on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                }
                else
                {
                    _log.Error($"Failed to place a bid on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                }
            }
            catch (Exception exception)
            {
                _log.Error($"Failed to place a bid on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                _log.Error(exception);
            }
        }

        public void AutoSell(string exchange, string symbol, string baseSymbol, decimal? maxToAcquire = null)
        {
            _log.Info($"Beginning auto-sell on {exchange} for {symbol}-{baseSymbol}.");

            decimal PriceTick = 0.0000001m;
            decimal LotSize = 0.0001m;
            decimal minQuantity = 0.001m;// 1.0m;

            var minQuantityDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "COSS-ZEN-ETH", 0.1m },
                { "ETH-BTC", 0.01m },
            };

            var tradingPairs = _exchangeClient.GetTradingPairs(exchange, CachePolicy.AllowCache);
            var tradingPair = tradingPairs.Single(item =>
            string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(item.BaseSymbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase));

            if (tradingPair.LotSize.HasValue)
            {
                LotSize = tradingPair.LotSize.Value;
            }

            if (tradingPair.PriceTick.HasValue)
            {
                PriceTick = tradingPair.PriceTick.Value;
            }

            var exchangePairKey = $"{exchange.ToUpper()}-{symbol.ToUpper()}-{baseSymbol.ToUpper()}";

            var pairKey = $"{symbol.ToUpper()}-{baseSymbol.ToUpper()}";
            if (minQuantityDictionary.ContainsKey(exchangePairKey))
            {
                minQuantity = minQuantityDictionary[exchangePairKey];
            }
            else if (minQuantityDictionary.ContainsKey(pairKey))
            {
                minQuantity = minQuantityDictionary[pairKey];
            }

            OrderBook orderBook = null;

            // CancelExistingOrders(exchange, symbol, baseSymbol);
            var openOrdersWithAsOf = GetOpenOrdersWithRetries(exchange, symbol, baseSymbol, CachePolicy.ForceRefresh);
            var openOrders = openOrdersWithAsOf?.OpenOrders ?? new List<OpenOrder>();
            var openAsks = openOrders.Where(item => item.OrderType == OrderType.Ask).ToList();
            if (openAsks.Count == 2)
            {
                foreach (var openAsk in openAsks)
                {
                    CancelOrderWithRetries(exchange, openAsk.OrderId);
                }
            }
            else if (openAsks.Count == 1)
            {
                var openAsk = openAsks.Single();

                orderBook = GetOrderBookWithRetries(exchange, symbol, baseSymbol, CachePolicy.ForceRefresh);
                var existingBestAsk = orderBook.BestAsk();
                var existingBestAskPrice = existingBestAsk.Price;
                if (existingBestAskPrice == openAsk.Price)
                {
                    _log.Info($"Our open ask on {exchange} for {symbol}-{baseSymbol} already has the best ask price of {openAsk.Price}.");
                    return;
                }

                CancelOrderWithRetries(exchange, openAsk.OrderId);

                // it would be better to remove the cancelled order from the order book,
                // but that involves comparing the quantities and adjusting for roudning.
                // reloading is safer for now.
                orderBook = null;
            }

            if (orderBook == null)
            {
                orderBook = GetOrderBookWithRetries(exchange, symbol, baseSymbol, CachePolicy.ForceRefresh);
            }

            var balances = GetBalancesWithRetries(exchange, new List<string> { symbol, baseSymbol }, CachePolicy.ForceRefresh);
            var symbolBalance = balances.FirstOrDefault(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase));
            var baseSymbolBalance = balances.FirstOrDefault(item => string.Equals(item.Symbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase));
            var availableSymbolBalance = symbolBalance?.Available ?? 0;
            var baseSymbolTotalBalance = baseSymbolBalance?.Total ?? 0;

            var bestAsk = orderBook.BestAsk();
            var bestAskPrice = bestAsk.Price;

            if (bestAskPrice <= 0) { throw new ApplicationException($"{exchange} best ask price for {symbol} should be > 0."); }
            var priceToSell = bestAskPrice - PriceTick;

            var quantityToSell = MathUtil.ConstrainToMultipleOf(availableSymbolBalance, LotSize);
            if (quantityToSell < minQuantity) { return; }

            if (maxToAcquire.HasValue)
            {
                var remainingNeeded = maxToAcquire - baseSymbolTotalBalance;
                if (remainingNeeded < LotSize) { return; }
                var askQuantityThatWouldGiveUsTheRemainingNeeded = remainingNeeded / priceToSell;
                if (askQuantityThatWouldGiveUsTheRemainingNeeded > quantityToSell)
                {
                    quantityToSell = MathUtil.ConstrainToMultipleOf(quantityToSell, LotSize);
                }
            }

            try
            {
                _log.Info($"About to place a sell limit for {quantityToSell} {symbol} on {exchange} for {priceToSell} {baseSymbol}.");
                var orderResult = _exchangeClient.SellLimit(exchange, symbol, baseSymbol, new QuantityAndPrice
                {
                    Price = priceToSell,
                    Quantity = quantityToSell
                });

                if (orderResult)
                {
                    _log.Info($"Successfully placed an ask for {quantityToSell} {symbol} on {exchange} for {priceToSell} {baseSymbol}.");
                }
                else
                {
                    _log.Error($"Failed to place an ask for {quantityToSell} {symbol} on {exchange} for {priceToSell} {baseSymbol}.");
                }
            }
            catch (Exception exception)
            {
                _log.Error($"Failed to place an ask for {quantityToSell} {symbol} on {exchange} for {priceToSell} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                _log.Error(exception);
            }
        }

        public void AutoStraddle(string exchange, string symbol, string baseSymbol)
        {
            var PriceTick = string.Equals(baseSymbol, "ETH", StringComparison.InvariantCultureIgnoreCase) ? 0.0000001m : 0.00000001m;
            const decimal LotSize = 0.0001m;
            const decimal MinSymbolTradeQuantity = 1.0m;

            var maxToOwnDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "CAN", 3000.0m },
                { "VNX", 15000.0m },
                { "BTT", 1000.0m }
            };

            var MaxAmountToOwn = maxToOwnDictionary[symbol];

            var MaxQuantityToSell = MaxAmountToOwn / 2.0m;

            var quantityToBuyDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "CAN", 500.0m },
                { "VNX", 2000.0m },
                { "BTT", 100.0m }
            };

            decimal QuantityToBuy = quantityToBuyDictionary[symbol];
            const decimal MinGapPercent = 2.0m;

            OrderBook orderBook = null;
            Holding symbolBalance = null;
            bool shouldClearOrderBook = false;
            
            var openOrdersWithAsOf = GetOpenOrdersWithRetries(exchange, symbol, baseSymbol, CachePolicy.ForceRefresh);
            var openOrders = openOrdersWithAsOf?.OpenOrders ?? new List<OpenOrder>();
            var openBids = openOrders.Where(item => item.OrderType == OrderType.Bid).ToList();
            if (openBids.Count == 2)
            {
                foreach (var openBid in openBids)
                {
                    CancelOrderWithRetries(exchange, openBid.OrderId);
                }

                openBids.Clear();
            }
            else if (openBids.Count == 1)
            {
                var openBid = openBids.Single();

                orderBook = GetOrderBookWithRetries(exchange, symbol, baseSymbol, CachePolicy.ForceRefresh);
                var existingBestBid = orderBook.BestBid();
                var existingBestBidPrice = existingBestBid.Price;
                if (existingBestBidPrice == openBid.Price
                    && (orderBook.Bids.Any(queryBid => queryBid.Price == openBid.Price - PriceTick))
                    && (openBid.Quantity >= QuantityToBuy * 0.75m))
                {
                        _log.Info($"Our open bid on {exchange} for {symbol}-{baseSymbol} already has the best bid price of {openBid.Price} and there's a bid directly below ours.");
                }
                else
                {
                    CancelOrderWithRetries(exchange, openBid.OrderId);
                    openBids.Clear();

                    var matchingOrderBookEntry = orderBook.Bids.Where(item => item.Price == openBid.Price && item.Quantity == openBid.Quantity)
                            .FirstOrDefault();

                    if (matchingOrderBookEntry != null)
                    {
                        orderBook.Bids.Remove(matchingOrderBookEntry);
                    }
                    else
                    {
                        shouldClearOrderBook = true;
                    }
                }
            }

            var openAsks = openOrders.Where(item => item.OrderType == OrderType.Ask).ToList();
            if (openAsks.Count == 2)
            {
                foreach (var openAsk in openAsks)
                {
                    CancelOrderWithRetries(exchange, openAsk.OrderId);
                }

                openAsks.Clear();
            }
            else if (openAsks.Count == 1)
            {
                var openAsk = openAsks.Single();

                if (orderBook == null)
                {
                    orderBook = GetOrderBookWithRetries(exchange, symbol, baseSymbol, CachePolicy.ForceRefresh);
                }
                var existingBestAsk = orderBook.BestAsk();
                var existingBestAskPrice = existingBestAsk.Price;

                if (existingBestAskPrice == openAsk.Price
                    && (orderBook.Asks.Any(queryAsk => queryAsk.Price == openAsk.Price + PriceTick)))
                {
                    _log.Info($"Our open ask on {exchange} for {symbol}-{baseSymbol} already has the best ask price of {openAsk.Price}.");
                }
                else
                {
                    CancelOrderWithRetries(exchange, openAsk.OrderId);
                    openAsks.Clear();

                    var matchingOrderBookEntry = orderBook.Asks.Where(item => item.Price == openAsk.Price && item.Quantity == openAsk.Quantity)
                            .FirstOrDefault();

                    if (matchingOrderBookEntry != null)
                    {
                        orderBook.Asks.Remove(matchingOrderBookEntry);
                    }
                    else
                    {
                        shouldClearOrderBook = true;
                    }              
                }
            }

            if (shouldClearOrderBook)
            {
                orderBook = null;
            }

            if (orderBook == null)
            {
                orderBook = GetOrderBookWithRetries(exchange, symbol, baseSymbol, CachePolicy.ForceRefresh);
            }

            if (symbolBalance == null)
            {
                symbolBalance = GetBalanceWithRetries(exchange, symbol, CachePolicy.ForceRefresh);
            }

            var bestAsk = orderBook.BestAsk();
            var bestAskPrice = bestAsk.Price;
            if (bestAskPrice <= 0) { throw new ApplicationException($"Best ask price on {exchange} for {symbol}-{baseSymbol} should be > 0."); }

            var bestBid = orderBook.BestBid();
            var bestBidPrice = bestBid.Price;
            if (bestBidPrice <= 0) { throw new ApplicationException($"Best bid price on {exchange} for {symbol}-{baseSymbol} should be > 0."); }

            var gapFlat = bestAskPrice - bestBidPrice;
            var gapRatio = gapFlat / bestBidPrice;
            var gapPercentDiff = 100.0m * gapRatio;

            if (gapPercentDiff < MinGapPercent)
            {
                if (openBids != null && openBids.Any())
                {
                    foreach(var openOrder in openBids)
                    {
                        try
                        {
                            _exchangeClient.CancelOrder(exchange, openOrder.OrderId);
                        }
                        catch(Exception exception)
                        {
                            _log.Error(exception);
                        }
                    }
                }

                if (openAsks != null && openAsks.Any())
                {
                    foreach (var openOrder in openAsks)
                    {
                        try
                        {
                            _exchangeClient.CancelOrder(exchange, openOrder.OrderId);
                        }
                        catch (Exception exception)
                        {
                            _log.Error(exception);
                        }
                    }
                }
            }
            else
            {
                if (openBids == null || !openBids.Any())
                {
                    var priceToBid = bestBidPrice + PriceTick;

                    if (symbolBalance.Total < MaxAmountToOwn)
                    {
                        var quantity = QuantityToBuy;
                        var price = priceToBid;

                        _log.Info($"About to place a buy limit order on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                        try
                        {
                            var orderResult = _exchangeClient.BuyLimit(exchange, symbol, baseSymbol, new QuantityAndPrice
                            {
                                Price = price,
                                Quantity = quantity
                            });
                            if (orderResult)
                            {
                                _log.Info($"Successfully placed a buy limit order on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                            }
                            else
                            {
                                _log.Error($"Failed to place a buy limit order on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");

                            }
                        }
                        catch (Exception exception)
                        {
                            _log.Error($"Failed to place a buy limit order on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                            _log.Error(exception);
                        }
                    }
                }

                if (openAsks == null || !openAsks.Any())
                {
                    var availableSymbolBalance = symbolBalance?.Available ?? 0;
                    if (availableSymbolBalance >= MinSymbolTradeQuantity)
                    {
                        var quantityToSellBeforeLotSize = availableSymbolBalance <= MaxQuantityToSell
                            ? availableSymbolBalance
                            : MaxQuantityToSell;

                        var quantity = MathUtil.ConstrainToMultipleOf(quantityToSellBeforeLotSize, LotSize);
                        var price = bestAskPrice - PriceTick;

                        _log.Info($"About to place a sell limit order on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                        try
                        {
                            var orderResult = _exchangeClient.SellLimit(exchange, symbol, baseSymbol, new QuantityAndPrice
                            {
                                Price = price,
                                Quantity = quantity
                            });
                            if (orderResult)
                            {
                                _log.Info($"Successfully placed a sell limit order on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                            }
                            else
                            {
                                _log.Error($"Failed to place a sell limit order on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                            }
                        }
                        catch (Exception exception)
                        {
                            _log.Error($"Failed to place a sell limit order on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                            _log.Error(exception);
                        }
                    }
                }
            }
        }

        public void AcquireUsdcEth()
        {
            const decimal PriceTick = 0.0001m;

            const decimal BidQuantity = 0.025m;
            const decimal UsdcMaxAcquire = 1000.0m;
            const decimal UsdcBalanceWhereWeCanSell = 250.0m;

            const string Symbol = "ETH";
            const string BaseSymbol = "USDC";

            OrderBook binanceUsdcBtcOrderBook = null;
            OrderBook binanceEthBtcOrderBook = null;
            var binanceTask = LongRunningTask.Run(() =>
            {
                binanceUsdcBtcOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, "USDC", "BTC", CachePolicy.ForceRefresh);
                binanceEthBtcOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, "ETH", "BTC", CachePolicy.ForceRefresh);
            });

            CancelExistingOrders(IntegrationNameRes.KuCoin, Symbol, BaseSymbol);

            var kucoinEthUsdcOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.KuCoin, Symbol, BaseSymbol, CachePolicy.ForceRefresh);
            binanceTask.Wait();

            var binanceEthBtcBestBid = binanceEthBtcOrderBook.BestBid();
            var binanceEthBtcBestBidPrice = binanceEthBtcBestBid.Price;
            if (binanceEthBtcBestBidPrice <= 0) { throw new ApplicationException("Binance's ETH-BTC best bid price should be > 0."); }

            var binanceEthBtcBestAsk = binanceEthBtcOrderBook.BestAsk();
            var binanceEthBtcBestAskPrice = binanceEthBtcBestAsk.Price;
            if (binanceEthBtcBestAskPrice <= 0) { throw new ApplicationException("Binance's ETH-BTC best ask price should be > 0."); }

            if (binanceEthBtcBestBidPrice >= binanceEthBtcBestAskPrice) { throw new ApplicationException("Binance's ETH-BTC best bid price must be lower than its best ask price."); }

            var ethBtcRatio = (binanceEthBtcBestBidPrice * binanceEthBtcBestBid.Quantity + binanceEthBtcBestAskPrice * binanceEthBtcBestAsk.Quantity) / (binanceEthBtcBestBid.Quantity + binanceEthBtcBestAsk.Quantity);
            var binanceBtcUsdcOrderBook = InvertOrderBook(binanceUsdcBtcOrderBook);
            var binanceBtcUsdcBestBidPrice = binanceBtcUsdcOrderBook.BestBid().Price;
            if (binanceBtcUsdcBestBidPrice <= 0) { throw new ApplicationException("Binance's BTC-USDC best bid price must be > 0."); }

            var binanceBtcUsdcBestAskPrice = binanceBtcUsdcOrderBook.BestAsk().Price;
            if (binanceBtcUsdcBestAskPrice <= 0) { throw new ApplicationException("Binance's BTC-USDC best ask price must be > 0."); }

            var binanceEthUsdcBestBidPrice = binanceBtcUsdcBestBidPrice * ethBtcRatio;
            var binanceEthUsdcBestAskPrice = binanceBtcUsdcBestAskPrice * ethBtcRatio;

            var kucoinEthUsdcBestBidPrice = kucoinEthUsdcOrderBook.BestBid().Price;
            if (kucoinEthUsdcBestBidPrice <= 0) { throw new ApplicationException($"Kucoin's {Symbol}-{BaseSymbol} best bid price should be > 0."); }

            var kucoinEthUsdcBestAskPrice = kucoinEthUsdcOrderBook.BestAsk().Price;
            if (kucoinEthUsdcBestAskPrice <= 0) { throw new ApplicationException($"Kucoin's {Symbol}-{BaseSymbol} best ask price should be > 0."); }

            if (kucoinEthUsdcBestBidPrice >= kucoinEthUsdcBestAskPrice)
            {
                throw new ApplicationException($"Kucoin's {Symbol}-{BaseSymbol} best bid price should be less than its best ask price.");
            }

            var balances = _exchangeClient.GetBalances(IntegrationNameRes.KuCoin, new List<string> { "USDC", "ETH" }, CachePolicy.ForceRefresh);
            var ethBalance = balances.SingleOrDefault(item => string.Equals(item.Symbol, "ETH", StringComparison.InvariantCultureIgnoreCase));

            var usdcBalance = balances.SingleOrDefault(item => string.Equals(item.Symbol, "USDC", StringComparison.InvariantCultureIgnoreCase));
            var usdcTotalBalance = usdcBalance?.Total ?? 0;

            if (usdcTotalBalance < UsdcMaxAcquire)
            {                
                var upTickBidPrice = kucoinEthUsdcBestBidPrice + PriceTick;
                var diff = binanceEthUsdcBestBidPrice - upTickBidPrice;
                var diffRatio = diff / binanceEthUsdcBestBidPrice;
                var percentDiff = 100.0m * diffRatio;

                if (percentDiff >= OpenBidMinimumPercentDiff)
                {
                    var quantity = BidQuantity;

                    var price = upTickBidPrice;

                    _log.Info($"About to place a bid on Kucoin for {quantity} {Symbol} at {price} {BaseSymbol}.");
                    try
                    {
                        var orderResult = _exchangeClient.BuyLimitV2(IntegrationNameRes.KuCoin, Symbol, BaseSymbol, new QuantityAndPrice
                        {
                            Quantity = quantity,
                            Price = upTickBidPrice
                        });

                        if (orderResult?.WasSuccessful ?? false)
                        {
                            _log.Info($"Successfully placed a bid on Kucoin for {quantity} {Symbol} at {price} {BaseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place a bid on Kucoin for {quantity} {Symbol} at {price} {BaseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place a bid on Kucoin for {quantity} {Symbol} at {price} {BaseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
            }
        }

        public void AutoEthXusd(string exchange, string dollarSymbol, bool shouldBuy, bool shouldSell)
        {
            const decimal IdealBidPercentDiff = 15.0m;
            const decimal MinBidPercentDiff = 5.0m;
            const decimal MinPercentDiffToKeepBid = 3.5m;

            const decimal IdealQuantityToBid = 1.0m;

            const string Symbol = "ETH";
            string baseSymbol = dollarSymbol;

            decimal? ethValuationResponse = null;
            var valuationTask = LongRunningTask.Run(() =>
            {
                ethValuationResponse = _workflowClient.GetUsdValue("ETH", CachePolicy.ForceRefresh);
            });

            var tradingPairs = _exchangeClient.GetTradingPairs(exchange, CachePolicy.AllowCache);
            var tradingPair = tradingPairs.Single(item =>
                string.Equals(item.Symbol, Symbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.BaseSymbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase));

            if (!tradingPair.LotSize.HasValue) { throw new ApplicationException($"Lot size not specified for {exchange}'s {Symbol}-{baseSymbol} pair."); }
            var lotSize = tradingPair.LotSize.Value;
            if (lotSize <= 0) { throw new ApplicationException($"Lot size for {exchange}'s {Symbol}-{baseSymbol} pair must be > 0."); }

            if (!tradingPair.PriceTick.HasValue) { throw new ApplicationException($"Price tick not specified for {exchange}'s {Symbol}-{baseSymbol} pair."); }
            var priceTick = tradingPair.PriceTick.Value;
            if (priceTick <= 0) { throw new ApplicationException($"Price tick for {exchange}'s {Symbol}-{baseSymbol} pair must be > 0."); }

            OrderBook arbEthXusdOrderBook = null;
            decimal arbEthXusdBestBidPrice;
            decimal arbEthXusdBestAskPrice;

            var openOrdersWithAskOf = _exchangeClient.GetOpenOrdersForTradingPairV2(exchange, Symbol, baseSymbol, CachePolicy.ForceRefresh);
            valuationTask.Wait();
            var ethValue = ethValuationResponse.Value;
            if (ethValue <= 0) { throw new ApplicationException("ETH's value should be > 0."); }

            var openOrders = openOrdersWithAskOf?.OpenOrders ?? new List<OpenOrder>();
            var openAsks = openOrders.Where(item => item.OrderType == OrderType.Ask).ToList();
            foreach (var openAsk in openAsks) { _exchangeClient.CancelOrder(exchange, openAsk); }

            OpenOrder existingOpenBid = null;
            var openBids = openOrders.Where(item => item.OrderType == OrderType.Bid).ToList();
            if (openBids.Count >= 2)
            {
                foreach (var openBid in openBids) { _exchangeClient.CancelOrder(exchange, openBid); }
            }
            else if (openBids.Count == 1)
            {
                arbEthXusdOrderBook = GetOrderBookWithRetries(exchange, Symbol, baseSymbol, CachePolicy.ForceRefresh);
                arbEthXusdBestBidPrice = arbEthXusdOrderBook.BestBid().Price;
                if (arbEthXusdBestBidPrice <= 0) { throw new ApplicationException($"{exchange}'s {Symbol}-{baseSymbol} best bid price should be > 0."); }

                arbEthXusdBestAskPrice = arbEthXusdOrderBook.BestAsk().Price;
                if (arbEthXusdBestAskPrice <= 0) { throw new ApplicationException($"{exchange}'s {Symbol}-{baseSymbol} best ask price should be > 0."); }
                if (arbEthXusdBestBidPrice >= arbEthXusdBestAskPrice) { throw new ApplicationException($"{exchange}'s {Symbol}-{baseSymbol} best ask price should be greater than its best bid price."); }

                existingOpenBid = openBids.Single();
                var shouldCancel = false;
                if (existingOpenBid.Price < arbEthXusdBestBidPrice)
                {
                    shouldCancel = true;
                }

                var diff = ethValue - existingOpenBid.Price;
                var diffRatio = diff / ethValue;
                var percentDiff = 100.0m * diffRatio;
                if (percentDiff < MinPercentDiffToKeepBid)
                {
                    shouldCancel = true;
                }

                if (shouldCancel)
                {
                    _exchangeClient.CancelOrder(exchange, existingOpenBid);
                    var matchingOrder = arbEthXusdOrderBook.Bids.FirstOrDefault(item => item.Quantity == existingOpenBid.Quantity && item.Price == existingOpenBid.Price);
                    if (matchingOrder != null)
                    {

                    }
                    else
                    {
                        arbEthXusdOrderBook = null;
                    }
                }
            }

            if (arbEthXusdOrderBook == null)
            {
                arbEthXusdOrderBook = GetOrderBookWithRetries(exchange, Symbol, baseSymbol, CachePolicy.ForceRefresh);
            }

            arbEthXusdBestBidPrice = arbEthXusdOrderBook.BestBid().Price;
            if (arbEthXusdBestBidPrice <= 0) { throw new ApplicationException($"{exchange}'s {Symbol}-{baseSymbol} best bid price should be > 0."); }

            arbEthXusdBestAskPrice = arbEthXusdOrderBook.BestAsk().Price;
            if (arbEthXusdBestAskPrice <= 0) { throw new ApplicationException($"{exchange}'s {Symbol}-{baseSymbol} best ask price should be > 0."); }
            if (arbEthXusdBestBidPrice >= arbEthXusdBestAskPrice) { throw new ApplicationException($"{exchange}'s {Symbol}-{baseSymbol} best ask price should be greater than its best bid price."); }

            var balances = _exchangeClient.GetBalances(exchange, CachePolicy.ForceRefresh);
            var ethBalance = balances.Holdings.SingleOrDefault(item => string.Equals(item.Symbol, Symbol, StringComparison.InvariantCultureIgnoreCase));
            var ethAvailable = ethBalance?.Available ?? 0;
            var xusdBalance = balances.Holdings.SingleOrDefault(item => string.Equals(item.Symbol, dollarSymbol, StringComparison.InvariantCultureIgnoreCase));
            var xusdAvailable = xusdBalance?.Available ?? 0;

            // Bid
            if (shouldBuy)
            {
                decimal? bidPriceToPlace = null;

                var idealBidPrice = MathUtil.ConstrainToMultipleOf(ethValue * (100.0m - IdealBidPercentDiff) / 100.0m, priceTick);
                if (idealBidPrice > arbEthXusdBestBidPrice)
                {
                    bidPriceToPlace = idealBidPrice;
                }
                else
                {
                    var upTickBidPrice = arbEthXusdBestBidPrice + priceTick;
                    var diff = upTickBidPrice - ethValue;
                    var ratio = diff / ethValue;
                    var percentDiff = 100.0m * ratio;

                    if (percentDiff >= MinBidPercentDiff)
                    {
                        bidPriceToPlace = upTickBidPrice;
                    }
                }

                var quantityToBid = MathUtil.ConstrainToMultipleOf(IdealQuantityToBid, lotSize);

                if (bidPriceToPlace.HasValue)
                {
                    var price = bidPriceToPlace.Value;
                    var quantity = quantityToBid;

                    _log.Info($"About to place a bid on {exchange} for {quantity} {Symbol} at {price} {baseSymbol}.");
                    try
                    {
                        var orderResult = _exchangeClient.BuyLimit(exchange, Symbol, baseSymbol, new QuantityAndPrice
                        {
                            Price = price,
                            Quantity = quantity
                        });

                        if (orderResult)
                        {
                            _log.Info($"Successfully placed a bid on {exchange} for {quantity} {Symbol} at {price} {baseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place a bid on {exchange} for {quantity} {Symbol} at {price} {baseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place a bid on {exchange} for {quantity} {Symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
            }

            if (shouldSell)
            {
                decimal? askPriceToPlace = null;
                var idealAskPrice = MathUtil.ConstrainToMultipleOf(ethValue * 1.15m, priceTick);
                if (idealAskPrice < arbEthXusdBestAskPrice)
                {
                    askPriceToPlace = idealAskPrice;
                }

                var quantityToAsk = MathUtil.ConstrainToMultipleOf(0.25m, lotSize);

                if (askPriceToPlace.HasValue)
                {
                    var price = askPriceToPlace.Value;
                    var quantity = quantityToAsk;

                    _log.Info($"About to place an ask on {exchange} for {quantity} {Symbol} at {price} {baseSymbol}.");
                    try
                    {
                        var orderResult = _exchangeClient.SellLimit(exchange, Symbol, baseSymbol, new QuantityAndPrice
                        {
                            Price = price,
                            Quantity = quantity
                        });

                        if (orderResult)
                        {
                            _log.Info($"Successfully placed a ask on {exchange} for {quantity} {Symbol} at {price} {baseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place an ask on {exchange} for {quantity} {Symbol} at {price} {baseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place an ask on {exchange} for {quantity} {Symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
            }
        }

        public void AutoXusd(string exchange, string dollarSymbol, string cryptoSymbol)
        {
            const decimal IdealBidPercentDiff = 7.5m;
            const decimal IdealAskPercentDiff = 7.5m;
            const decimal MinBidPercentDiff = 3.0m;
            const decimal MinAskPercentDiff = 3.75m;

            var miniumTradeQuantityDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "BTC", 0.001m },
                { "ETH", 0.030m },
            };

            var idealCryptoQuantityDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "BTC", 0.015m },
                { "ETH", 0.25m },
            };

            var minimumTradeQuantity = miniumTradeQuantityDictionary[cryptoSymbol];
            var idealCryptoQuantity = idealCryptoQuantityDictionary[cryptoSymbol];

            string symbol = cryptoSymbol;
            string baseSymbol = dollarSymbol;

            decimal? symbolValuationResponse = null;
            var valuationTask = LongRunningTask.Run(() =>
            {
                symbolValuationResponse = _workflowClient.GetUsdValue(symbol, CachePolicy.ForceRefresh);
            });

            var arbTradingPairs = _exchangeClient.GetTradingPairs(exchange, CachePolicy.AllowCache);
            var tradingPair = arbTradingPairs.Single(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.BaseSymbol, dollarSymbol, StringComparison.InvariantCultureIgnoreCase));

            if (!tradingPair.LotSize.HasValue) { throw new ApplicationException($"{exchange}'s {symbol}-{baseSymbol} trading pair does not have a lot size configured."); }
            var lotSize = tradingPair.LotSize.Value;

            if (!tradingPair.PriceTick.HasValue) { throw new ApplicationException($"{exchange}'s {symbol}-{baseSymbol} trading pair does not have a price tick configured."); }
            var priceTick = tradingPair.PriceTick.Value;

            var arbBalances = _exchangeClient.GetBalances(exchange, CachePolicy.ForceRefresh);
            var symbolAvailable = arbBalances?.GetAvailableForSymbol(symbol) ?? 0;
            var xusdAvailable = arbBalances?.GetAvailableForSymbol(dollarSymbol) ?? 0;

            CancelOpenOrdersForTradingPair(exchange, symbol, baseSymbol);

            var arbSymbolXusdOrderbook = GetOrderBookWithRetries(exchange, symbol, baseSymbol, CachePolicy.ForceRefresh);
            var arbSymbolXusdBestBidPrice = arbSymbolXusdOrderbook.BestBid().Price;
            if (arbSymbolXusdBestBidPrice <= 0) { throw new ApplicationException($"{exchange}'s {symbol}-{baseSymbol} best bid price should be > 0."); }

            var arbSymbolXusdBestAskPrice = arbSymbolXusdOrderbook.BestAsk().Price;
            if (arbSymbolXusdBestAskPrice <= 0) { throw new ApplicationException($"{exchange}'s {symbol}-{baseSymbol} best ask price should be > 0."); }

            if (arbSymbolXusdBestBidPrice >= arbSymbolXusdBestAskPrice)
            {
                throw new ApplicationException($"{exchange}'s {symbol}-{baseSymbol} best ask price should be greater than its best bid price.");
            }

            valuationTask.Wait();

            var symbolValue = symbolValuationResponse.Value;
            if (symbolValue <= 0) { throw new ApplicationException($"{symbol}'s value should be > 0."); }

            decimal? bidPriceToPlace = null;

            // Bid
            var idealBidPrice = MathUtil.ConstrainToMultipleOf(symbolValue * (100.0m - IdealBidPercentDiff) / 100.0m, priceTick);
            if (idealBidPrice > arbSymbolXusdBestBidPrice)
            {
                bidPriceToPlace = idealBidPrice;
            }
            else
            {
                var upTickBidPrice = arbSymbolXusdBestBidPrice + priceTick;
                var diff = symbolValue - upTickBidPrice;
                var ratio = diff / symbolValue;
                var percentDiff = 100.0m * ratio;

                if (percentDiff >= MinBidPercentDiff)
                {
                    bidPriceToPlace = upTickBidPrice;
                }
            }

            if (bidPriceToPlace.HasValue)
            {
                var price = bidPriceToPlace.Value;

                var maxPossibleQuantityToBid = xusdAvailable / (price * 1.01m);
                var unconstrainedQuantityToBid = idealCryptoQuantity > maxPossibleQuantityToBid
                    ? maxPossibleQuantityToBid
                    : idealCryptoQuantity;

                var quantityToBid = MathUtil.ConstrainToMultipleOf(unconstrainedQuantityToBid, lotSize);

                if (quantityToBid >= minimumTradeQuantity)
                {
                    var quantity = quantityToBid;

                    _log.Info($"About to place a bid on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                    try
                    {
                        var orderResult = _exchangeClient.BuyLimitV2(exchange, symbol, baseSymbol, new QuantityAndPrice
                        {
                            Price = price,
                            Quantity = quantity
                        });

                        if (orderResult.WasSuccessful)
                        {
                            _log.Info($"Successfully placed a bid on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place a bid on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place a bid on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
            }

            decimal? askPriceToPlace = null;
            var idealAskPrice = MathUtil.ConstrainToMultipleOf(symbolValue * (100.0m + IdealAskPercentDiff) / 100.0m, priceTick);
            if (idealAskPrice < arbSymbolXusdBestAskPrice)
            {
                askPriceToPlace = idealAskPrice;
            }
            else
            {
                var downTickAskPrice = arbSymbolXusdBestAskPrice - priceTick;
                var diff = downTickAskPrice - symbolValue;
                var ratio = diff / symbolValue;
                var percentDiff = 100.0m * ratio;
                if (percentDiff >= MinAskPercentDiff)
                {
                    askPriceToPlace = downTickAskPrice;
                }
            }

            if (askPriceToPlace.HasValue)
            {
                var price = askPriceToPlace.Value;

                var maxPossiblAskQuantity = symbolAvailable / 1.01m;
                var unconstrainedQuantityToAsk = idealCryptoQuantity < maxPossiblAskQuantity
                    ? idealCryptoQuantity
                    : maxPossiblAskQuantity;

                var quantityToAsk = MathUtil.ConstrainToMultipleOf(unconstrainedQuantityToAsk, lotSize);

                if (quantityToAsk >= minimumTradeQuantity)
                {
                    var quantity = quantityToAsk;

                    _log.Info($"About to place an ask on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                    try
                    {
                        var orderResult = _exchangeClient.SellLimitV2(exchange, symbol, baseSymbol, new QuantityAndPrice
                        {
                            Price = price,
                            Quantity = quantity
                        });

                        if (orderResult.WasSuccessful)
                        {
                            _log.Info($"Successfully placed a ask on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place an ask on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place an ask on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
            }
        }

        public void AutoReverseXusd(string exchange, string dollarSymbol, string cryptoSymbol)
        {
            const decimal IdealBidPercentDiff = 7.5m;
            const decimal IdealAskPercentDiff = 7.5m;
            const decimal MinBidPercentDiff = 3.25m;
            const decimal MinAskPercentDiff = 3.25m;

            var idealCryptoQuantityDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "BTC", 0.015m },
                { "ETH", 0.25m },
            };           

            var minimumTradeQuantity = 1.0m;
            var idealCryptoQuantity = idealCryptoQuantityDictionary[cryptoSymbol];

            string symbol = dollarSymbol;
            string baseSymbol = cryptoSymbol; 

            decimal? baseSymbolValuationResponse = null;
            var valuationTask = LongRunningTask.Run(() =>
            {
                baseSymbolValuationResponse = _workflowClient.GetUsdValue(baseSymbol, CachePolicy.ForceRefresh);
            });

            var arbTradingPairs = _exchangeClient.GetTradingPairs(exchange, CachePolicy.AllowCache);
            var tradingPair = arbTradingPairs.Single(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.BaseSymbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase));

            if (!tradingPair.LotSize.HasValue) { throw new ApplicationException($"{exchange}'s {symbol}-{baseSymbol} trading pair does not have a lot size configured."); }
            var lotSize = tradingPair.LotSize.Value;

            if (!tradingPair.PriceTick.HasValue) { throw new ApplicationException($"{exchange}'s {symbol}-{baseSymbol} trading pair does not have a price tick configured."); }
            var priceTick = tradingPair.PriceTick.Value;

            var arbBalances = _exchangeClient.GetBalances(exchange, CachePolicy.ForceRefresh);
            var baseSymbolAvailable = arbBalances?.GetAvailableForSymbol(baseSymbol) ?? 0;
            var xusdTotal = arbBalances?.GetTotalForSymbol(dollarSymbol) ?? 0;
            var xusdAvailable = arbBalances?.GetAvailableForSymbol(dollarSymbol) ?? 0;
            var idealXusdQuantity = (xusdTotal >= 400.0m && xusdAvailable >= 275.0m) ? 200.0m : 100.0m;

            CancelOpenOrdersForTradingPair(exchange, symbol, baseSymbol);

            var arbXusdSymbolOrderbook = GetOrderBookWithRetries(exchange, symbol, baseSymbol, CachePolicy.ForceRefresh);
            var arbXusdSymbolBestBidPrice = arbXusdSymbolOrderbook.BestBid().Price;
            if (arbXusdSymbolBestBidPrice <= 0) { throw new ApplicationException($"{exchange}'s {symbol}-{baseSymbol} best bid price should be > 0."); }

            var arbXusdSymbolBestAskPrice = arbXusdSymbolOrderbook.BestAsk().Price;
            if (arbXusdSymbolBestAskPrice <= 0) { throw new ApplicationException($"{exchange}'s {symbol}-{baseSymbol} best ask price should be > 0."); }

            if (arbXusdSymbolBestBidPrice >= arbXusdSymbolBestAskPrice)
            {
                throw new ApplicationException($"{exchange}'s {symbol}-{baseSymbol} best ask price should be greater than its best bid price.");
            }

            valuationTask.Wait();

            var baseSymbolValue = baseSymbolValuationResponse.Value;
            if (baseSymbolValue <= 0) { throw new ApplicationException($"{symbol}'s value should be > 0."); }

            decimal? bidPriceToPlace = null;

            // Bid
            var idealBidPrice = MathUtil.ConstrainToMultipleOf((100.0m - IdealBidPercentDiff) / 100.0m /  baseSymbolValue, priceTick);
            if (idealBidPrice > arbXusdSymbolBestBidPrice)
            {
                bidPriceToPlace = idealBidPrice;
            }
            else
            {
                var upTickBidPrice = arbXusdSymbolBestBidPrice + priceTick;
                var diff = baseSymbolValue - upTickBidPrice;
                var ratio = diff / baseSymbolValue;
                var percentDiff = 100.0m * ratio;

                if (percentDiff >= MinBidPercentDiff)
                {
                    bidPriceToPlace = upTickBidPrice;
                }
            }

            if (bidPriceToPlace.HasValue)
            {
                var price = bidPriceToPlace.Value;

                var maxPossibleQuantityToBid = baseSymbolAvailable / (price * 1.01m);
                var unconstrainedQuantityToBid = idealXusdQuantity > maxPossibleQuantityToBid
                    ? maxPossibleQuantityToBid
                    : idealXusdQuantity;

                var quantityToBid = MathUtil.ConstrainToMultipleOf(unconstrainedQuantityToBid, lotSize);

                if (quantityToBid >= minimumTradeQuantity)
                {
                    var quantity = quantityToBid;

                    _log.Info($"About to place a bid on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                    try
                    {
                        var orderResult = _exchangeClient.BuyLimitV2(exchange, symbol, baseSymbol, new QuantityAndPrice
                        {
                            Price = price,
                            Quantity = quantity
                        });

                        if (orderResult.WasSuccessful)
                        {
                            _log.Info($"Successfully placed a bid on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place a bid on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place a bid on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
            }

            decimal? askPriceToPlace = null;
            var idealAskPrice = MathUtil.ConstrainToMultipleOf((100.0m + IdealAskPercentDiff) / 100.0m / baseSymbolValue, priceTick);
            if (idealAskPrice < arbXusdSymbolBestAskPrice)
            {
                askPriceToPlace = idealAskPrice;
            }
            else
            {
                var invertedValue = 1.0m / baseSymbolValue;

                var downTickAskPrice = arbXusdSymbolBestAskPrice - priceTick;
                var diff = downTickAskPrice - invertedValue;
                var ratio = diff / invertedValue;
                var percentDiff = 100.0m * ratio;
                if (percentDiff >= MinAskPercentDiff)
                {
                    askPriceToPlace = downTickAskPrice;
                }
            }

            if (askPriceToPlace.HasValue)
            {
                var price = askPriceToPlace.Value;

                var maxPossiblAskQuantity = xusdAvailable / 1.01m;
                var unconstrainedQuantityToAsk = idealXusdQuantity < maxPossiblAskQuantity
                    ? idealXusdQuantity
                    : maxPossiblAskQuantity;

                var quantityToAsk = MathUtil.ConstrainToMultipleOf(unconstrainedQuantityToAsk, lotSize);

                if (quantityToAsk >= minimumTradeQuantity)
                {
                    var quantity = quantityToAsk;

                    _log.Info($"About to place an ask on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                    try
                    {
                        var orderResult = _exchangeClient.SellLimitV2(exchange, symbol, baseSymbol, new QuantityAndPrice
                        {
                            Price = price,
                            Quantity = quantity
                        });

                        if (orderResult.WasSuccessful)
                        {
                            _log.Info($"Successfully placed a ask on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place an ask on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place an ask on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
            }
        }

        public void AcquireQuantity(string exchange, string symbol, string baseSymbol, decimal desiredQuantity)
        {
            var balances = _exchangeClient.GetBalances(exchange, CachePolicy.ForceRefresh);
            var totalForSymbol = balances.GetTotalForSymbol(symbol);
            var remainingQuantity = desiredQuantity - totalForSymbol;
            var ratioRemaining = remainingQuantity / desiredQuantity;
            var percentRemaining = 100.0m * ratioRemaining;

            if (percentRemaining <= 1.0m) { return; }

            var commodity = _exchangeClient.GetCommoditiyForExchange(exchange, symbol, null, CachePolicy.AllowCache);
            var tradingPairs = _exchangeClient.GetTradingPairs(exchange, CachePolicy.AllowCache);
            var tradingPair = tradingPairs.Single(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.BaseSymbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase));

            var lotSize = tradingPair.LotSize.Value;
            if (lotSize <= 0) { throw new ApplicationException($"{exchange}'s lot size for {symbol}-{baseSymbol} must be > 0."); }

            var priceTick = tradingPair.PriceTick.Value;
            if (priceTick <= 0) { throw new ApplicationException($"{exchange}'s price tick for {symbol}-{baseSymbol} must be > 0."); }

            var openOrdersResult = _exchangeClient.GetOpenOrdersForTradingPairV2(IntegrationNameRes.Binance, symbol, baseSymbol, CachePolicy.ForceRefresh);
            foreach (var openOrder in openOrdersResult?.OpenOrders ?? new List<OpenOrder>())
            {
                _exchangeClient.CancelOrder(exchange, openOrder);
            }

            var orderBook = _exchangeClient.GetOrderBook(exchange, symbol, baseSymbol, CachePolicy.ForceRefresh);

            var bestBidPrice = orderBook.BestBid().Price;
            if (bestBidPrice <= 0) { throw new ApplicationException($"{exchange}'s {symbol}-{baseSymbol} best bid price should be > 0."); }

            var bidPriceToPlace = bestBidPrice + priceTick;
            var quantity = MathUtil.ConstrainToMultipleOf(remainingQuantity, lotSize);

            var price = bidPriceToPlace;

            try
            {
                _log.Info($"About to place a bid on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                var orderResult = _exchangeClient.BuyLimitV2(exchange, symbol, baseSymbol, new QuantityAndPrice
                {
                    Price = price,
                    Quantity = quantity
                });

                if (orderResult.WasSuccessful)
                {
                    _log.Info($"Successfully placed a bid on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                }
                else
                {
                    _log.Error($"Failed to place a bid on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.");
                }
            }
            catch (Exception exception)
            {
                _log.Error($"Failed to place a bid on {exchange} for {quantity} {symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                _log.Error(exception);
            }
        }

        public void AutoEthBtc(string exchange)
        {
            const string Symbol = "ETH";
            const string BaseSymbol = "BTC";
            const decimal MinPercentDiff = 1.0m;

            OrderBook binanceEthBtcOrderBook = null;
            var binanceTask = LongRunningTask.Run(() => binanceEthBtcOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, "ETH", "BTC", CachePolicy.ForceRefresh));

            var tradingPairs = _exchangeClient.GetTradingPairs(exchange, CachePolicy.AllowCache);
            var tradingPair = tradingPairs.Single(queryTradingPair =>
                string.Equals(queryTradingPair.Symbol, Symbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(queryTradingPair.BaseSymbol, BaseSymbol, StringComparison.InvariantCultureIgnoreCase));

            var openOrders = _exchangeClient.GetOpenOrdersForTradingPairV2(exchange, Symbol, BaseSymbol, CachePolicy.ForceRefresh)?.OpenOrders ?? new List<OpenOrder>();
            var openAsks = openOrders.Where(queryOpenOrder => queryOpenOrder.OrderType == OrderType.Ask).ToList();
            var openBids = openOrders.Where(queryOpenOrder => queryOpenOrder.OrderType == OrderType.Bid).ToList();
            foreach (var openOrder in openBids) { _exchangeClient.CancelOrder(exchange, openOrder); }

            OpenOrder existingOpenAsk = null;
            if (openAsks.Count >= 2)
            {
                foreach (var openOrder in openBids) { _exchangeClient.CancelOrder(exchange, openOrder); }
            }
            else if(openAsks.Count == 1)
            {
                existingOpenAsk = openAsks.Single();
            }

            var blocktradeEthBtcOrderBook = _exchangeClient.GetOrderBook(exchange, Symbol, BaseSymbol, CachePolicy.ForceRefresh);
            if (existingOpenAsk != null)
            {
                var matchingOrder = blocktradeEthBtcOrderBook.Asks.FirstOrDefault(queryOrder =>
                    queryOrder.Price == existingOpenAsk.Price
                    && queryOrder.Quantity == existingOpenAsk.Quantity);

                // If we can't find our open ask in the order book, cancel it and reload the order book.
                if (matchingOrder == null)
                {
                    _exchangeClient.CancelOrder(exchange, existingOpenAsk);
                    existingOpenAsk = null;
                    blocktradeEthBtcOrderBook = _exchangeClient.GetOrderBook(exchange, Symbol, BaseSymbol, CachePolicy.ForceRefresh);
                }
                else
                {
                    blocktradeEthBtcOrderBook.Asks.Remove(matchingOrder);
                }
            }

            binanceTask.Wait();

            var blocktradeBestAskPrice = blocktradeEthBtcOrderBook.BestAsk().Price;
            var binanceBestAskPrice = binanceEthBtcOrderBook.BestAsk().Price;

            decimal? askPriceToPlace = null;

            var idealAskPrice = MathUtil.ConstrainToMultipleOf(binanceBestAskPrice * 1.15m, tradingPair.PriceTick.Value);
            if (idealAskPrice < blocktradeBestAskPrice)
            {
                askPriceToPlace = blocktradeBestAskPrice;
            }
            else
            {
                var tickDownAskPrice = blocktradeBestAskPrice - tradingPair.PriceTick.Value;
                var diff = tickDownAskPrice - binanceBestAskPrice;
                var ratio = diff / tickDownAskPrice;
                var percentDiff = 100.0m * ratio;

                if (percentDiff >= MinPercentDiff)
                {
                    askPriceToPlace = tickDownAskPrice;
                }
            }

            var quantity = 0.25m;

            if (quantity > 0 && askPriceToPlace.HasValue && askPriceToPlace.Value > 0 && askPriceToPlace.Value > binanceBestAskPrice)
            {
                var price = askPriceToPlace.Value;

                var shouldPlaceOrder = true;
                if (existingOpenAsk != null)
                {
                    // does the existing open ask match the ask we're about to place?
                    // if so, keep it and don't place another ask.
                    if (existingOpenAsk.Price == price && existingOpenAsk.Quantity == quantity)
                    {
                        shouldPlaceOrder = false;
                    }
                    else
                    {
                        _exchangeClient.CancelOrder(exchange, existingOpenAsk);
                    }
                }

                if (shouldPlaceOrder)
                {
                    _log.Info($"About to place a limit ask on {exchange} for {quantity} {Symbol} at {price} {BaseSymbol}.");
                    try
                    {
                        var orderResult = _exchangeClient.SellLimitV2(exchange, Symbol, BaseSymbol, new QuantityAndPrice
                        {
                            Quantity = quantity,
                            Price = price
                        });

                        _log.Info($"Successfully placed a limit ask on {exchange} for {quantity} {Symbol} at {price} {BaseSymbol}.");
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place a limit ask on {exchange} for {quantity} {Symbol} at {price} {BaseSymbol}.", exception);
                    }
                }
            }

            Console.WriteLine(blocktradeEthBtcOrderBook);
        }

        private void CancelOpenOrdersForTradingPair(string exchange, string symbol, string baseSymbol)
        {
            var openOrdersWithAsOf = _exchangeClient.GetOpenOrdersForTradingPairV2(exchange, symbol, baseSymbol, CachePolicy.ForceRefresh);
            var openOrders = openOrdersWithAsOf?.OpenOrders ?? new List<OpenOrder>();
            foreach (var openOrder in openOrders)
            {
                _exchangeClient.CancelOrder(exchange, openOrder.OrderId);
            }
        }

        private static Dictionary<string, decimal> BaseSymbolOpenBidQuantityDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "ETH", 0.50m },
            { "BTC", 0.015m },
            { "COSS", 250.0m },
            { "QASH", 100.0m }
        };

        private static OrderBook InvertOrderBook(OrderBook originalOrderBook)
        {
            var asks = originalOrderBook?.Bids != null
                ? originalOrderBook.Bids.Select(item =>
                new Order
                {
                    Price = 1.0m / item.Price,
                    Quantity = item.Quantity
                }).ToList()
                : new List<Order>();

            var bids = originalOrderBook.Asks != null
                ? originalOrderBook.Asks.Select(item =>
                new Order
                {
                    Price = 1.0m / item.Price,
                    Quantity = item.Quantity
                }).ToList()
                : new List<Order>();

            return new OrderBook
            {
                Asks = asks,
                Bids = bids
            };
        }
    }
}
