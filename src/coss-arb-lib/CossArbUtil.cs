using cache_lib.Models;
using config_client_lib;
using log_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using task_lib;
using trade_model;
using trade_strategy_lib;
using workflow_client_lib;
using exchange_client_lib;
using math_lib;
using trade_constants;

namespace coss_arb_lib
{
    public partial class CossArbUtil : ICossArbUtil
    {
        private const decimal DefaultLotSize = 0.00000001m;
        private const decimal DefaultPriceTick = 0.00000001m;

        private const decimal OptimalPercentDiff = 15.0m;
        private const decimal MinOpenBidPercentDiff = 3.5m;
        private const decimal MinOpenAskPercentDiff = 2.5m;
        private const decimal MinInstantAskPercentDiff = 1.0m;
        private const decimal MinInstantBidPercentDiff = 1.5m;
        private const decimal MinTusdInstantBidPercentDiff = 3.0m;
        

        private static List<string> CossDisabledSymbols = new List<string>
        {
            // "ARK",
            // "WAVES",
            // "DASH",
            // "ZEN",
            // "ICX",
            // "EOS",
            // "LSK",
            "BPL",
            "SMDX",
            // "XEM",
            "QTUM"
        };

        // There's also a minimum trade quantity of 0.00006.
        // Not sure if it's possible to run into that limit
        // before running into the minimum eth/btc limit...

        private const decimal MinimumTradeEth = 0.021m;
        private const decimal MinimumTradeBtc = 0.00051m;
        private const decimal MinimumTradeCoss = 25.1m;               
        private const decimal MinimumTradeTusd = 2.1m;
        private const decimal MinimumTradeUsdt = 2.1m; // Just guessing on this value for now. Need to find the real value.
        private const decimal MinimumTradeXrp = 5.1m; // Just guessing on this value for now. Need to find the real value.
        private const decimal RoundingErrorPrevention = 1.00001m;

        private static Dictionary<string, decimal> BaseSymbolOpenQuantityDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "ETH", 0.25m },
            { "BTC", 0.0075m },
            { "COS", 200.0m }
        };

        private readonly IConfigClient _configClient;
        private readonly IExchangeClient _exchangeClient;
        private readonly IWorkflowClient _workflowClient;
        private readonly ILogRepo _log;

        public CossArbUtil(
            IConfigClient configClient,
            IExchangeClient exchangeClient,
            IWorkflowClient workflowClient,
            ILogRepo log)
        {
            _configClient = configClient;
            _exchangeClient = exchangeClient;
            _workflowClient = workflowClient;
            _log = log;

            _log.EnableConsole();
        }

        public void AutoBuy(CachePolicy cachePolicy)
        {
            var symbolsToAvoid = new List<string>
            {
                "ETH",
                "USD"
            }.Union(CossDisabledSymbols)
            .ToList();

            var baseSymbolsToAvoid = new List<string> { "USD" };

            var tradingPairsCachePolicy = cachePolicy == CachePolicy.ForceRefresh
                ? CachePolicy.AllowCache
                : cachePolicy;

            var cossTradingPairsTask = LongRunningTask.Run(() => _exchangeClient.GetTradingPairs(IntegrationNameRes.Coss, tradingPairsCachePolicy));
            var binanceTradingPairsTask = LongRunningTask.Run(() => _exchangeClient.GetTradingPairs(IntegrationNameRes.Binance, tradingPairsCachePolicy));

            var cossTradingPairs = cossTradingPairsTask.Result;
            var binanceTradingPairs = binanceTradingPairsTask.Result;

            var intersections = GetIntersections(cossTradingPairs, binanceTradingPairs);

            foreach(var intersection in intersections)
            {
                if (symbolsToAvoid.Any(querySymbol => string.Equals(intersection.Symbol, querySymbol, StringComparison.InvariantCultureIgnoreCase)))
                { continue; }

                if (baseSymbolsToAvoid.Any(queryBaseSymbol => string.Equals(intersection.BaseSymbol, queryBaseSymbol, StringComparison.InvariantCultureIgnoreCase)))
                { continue; }

                AutoBuy(intersection.Symbol, intersection.BaseSymbol, cachePolicy);
            }
        }

        public void AutoBuy(string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            var config = _configClient.GetCossAgentConfig();
            if (config == null) { throw new ApplicationException("Failed to get Coss-Agent config."); }
            if (!config.IsCossAutoTradingEnabled) { return; }
            const decimal MinimumTokenThreshold = 1.0m;
            var tokenThreshold = config.TokenThreshold >= MinimumTokenThreshold ? config.TokenThreshold : MinimumTokenThreshold;

            var nullableMinimumTrade = GetMinimumTradeForBaseSymbol(baseSymbol);
            if (!nullableMinimumTrade.HasValue) { return; }
            var minimumTrade = nullableMinimumTrade.Value;
            if (minimumTrade < 0) { throw new ApplicationException($"Minimum {baseSymbol} trade value must not be < 0."); }

            var baseSymbolHoldingCachePolicy = cachePolicy == CachePolicy.ForceRefresh
                    ? CachePolicy.AllowCache
                    : cachePolicy;

            Holding baseSymbolHolding = null;
            try
            {
                baseSymbolHolding = _exchangeClient.GetBalance(IntegrationNameRes.Coss, baseSymbol, baseSymbolHoldingCachePolicy);
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                Thread.Sleep(TimeSpan.FromSeconds(5));

                baseSymbolHolding = _exchangeClient.GetBalance(IntegrationNameRes.Coss, baseSymbol, baseSymbolHoldingCachePolicy);
            }

            var baseSymbolAvailable = baseSymbolHolding?.Available ?? 0;
            if (baseSymbolAvailable < minimumTrade) { return; }

            var cossOrderBookTask = LongRunningTask.Run(() => _exchangeClient.GetOrderBook(IntegrationNameRes.Coss, symbol, baseSymbol, cachePolicy));
            var binanceOrderBookTask = LongRunningTask.Run(() => _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, symbol, baseSymbol, cachePolicy));

            var binanceOrderBook = binanceOrderBookTask.Result;
            if (binanceOrderBook == null) { return; }

            var cossOrderBook = cossOrderBookTask.Result;
            if (cossOrderBook == null) { return; }

            var binanceBestBid = binanceOrderBook.BestBid();
            if (binanceBestBid == null) { return; }
            var binanceBestBidPrice = binanceBestBid.Price;

            var asksWithAtLeastSomeProfit = (cossOrderBook.Asks ?? new List<Order>())
                .Where(queryAsk =>
                {
                    var diff = binanceBestBidPrice - queryAsk.Price;
                    var diffRatio = diff / binanceBestBidPrice;
                    var percentDiff = 100.0m * diffRatio;

                    return percentDiff >= 0;
                })
                .ToList();

            if (!asksWithAtLeastSomeProfit.Any()) { return; }

            var viableAsks = (cossOrderBook.Asks ?? new List<Order>())
                .Where(queryAsk =>
                {
                    var diff = binanceBestBidPrice - queryAsk.Price;
                    var diffRatio = diff / binanceBestBidPrice;
                    var percentDiff = 100.0m * diffRatio;

                    return percentDiff >= tokenThreshold;
                })
                .ToList();

            if (!viableAsks.Any()) { return; }

            var quantityToBuy = viableAsks.Sum(queryAsk => queryAsk.Quantity);
            var priceToBuy = viableAsks.Max(queryAsk => queryAsk.Price) * RoundingErrorPrevention;

            var potentialTrade = quantityToBuy * priceToBuy;
            if (potentialTrade > baseSymbolAvailable)
            {
                // Round it down so that we don't accidentally round over what we have to spend.
                quantityToBuy = (baseSymbolAvailable / priceToBuy) / RoundingErrorPrevention;
            }

            if (potentialTrade < minimumTrade)
            {
                quantityToBuy = (minimumTrade / priceToBuy) * RoundingErrorPrevention;
                if (quantityToBuy * priceToBuy > baseSymbolAvailable) { return; }
            }

            try
            {
                var result = _exchangeClient.BuyLimit(IntegrationNameRes.Coss, symbol, baseSymbol, new QuantityAndPrice
                {
                    Quantity = quantityToBuy,
                    Price = priceToBuy
                });

                if (!result) { throw new ApplicationException($"Failed to buy {quantityToBuy} {symbol} from {IntegrationNameRes.Coss} at {priceToBuy} {baseSymbol}"); }
            }
            finally
            {
                var openOrders = _exchangeClient.GetOpenOrders(IntegrationNameRes.Coss, symbol, baseSymbol, CachePolicy.ForceRefresh);
                var openBids = (openOrders ?? new List<OpenOrderForTradingPair>()).Where(queryOpenOrder => queryOpenOrder.OrderType == OrderType.Bid).ToList();
                for (var i = 0; i < openBids.Count; i++)
                {
                    if (i != 0) { Thread.Sleep(TimeSpan.FromSeconds(2.5)); }
                    var openBid = openBids[i];
                    try
                    {
                        _exchangeClient.CancelOrder(IntegrationNameRes.Coss, openBid.OrderId);
                    }
                    catch (Exception exception)
                    {
                        _log.Error(exception);
                    }
                }
            }
        }

        public void AutoSell()
        {
            var tradingPairsCachePolicy = CachePolicy.AllowCache;

            var cossTradingPairsTask = LongRunningTask.Run(() => GetTradingPairsWithRetries(IntegrationNameRes.Coss, tradingPairsCachePolicy));
            var binanceTradingPairsTask = LongRunningTask.Run(() => GetTradingPairsWithRetries(IntegrationNameRes.Binance, tradingPairsCachePolicy));

            var cossTradingPairs = cossTradingPairsTask.Result;
            var binanceTradingPairs = binanceTradingPairsTask.Result;

            var intersections = GetIntersections(cossTradingPairs, binanceTradingPairs);

            var balances = GetCossBalancesWithRetries(CachePolicy.AllowCache);
            var usdValues = new Dictionary<string, decimal?>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var tradingPair in intersections)
            {
                // ETH-BTC should be traded differently
                // and we don't yet have clearance to trade USD
                if (new List<string> { "ETH", "USD" }.Any(item => string.Equals(item, tradingPair.Symbol, StringComparison.InvariantCultureIgnoreCase)))
                {
                    continue;
                }

                var balance = balances.GetHoldingForSymbol(tradingPair.Symbol);
                if (balance == null || balance.Available <= 0) { continue; }

                AutoSellTradingPair(tradingPair, balance);
            }
        }

        public List<AggregateOrderBookItem> AutoSellSymbol(string symbol)
        {
            var baseSymbols = new List<string> { "ETH", "BTC" };

            decimal? ethUsdResult = null;
            decimal? btcUsdResult = null;

            OrderBook cossEthOrderBook = null;
            OrderBook cossBtcOrderBook = null;

            OrderBook binanceBtcOrderBook = null;
            OrderBook binanceEthOrderBook = null;

            // Do the things that require auth synchronously first so that if it fails or we don't have enough, 
            // we can avoid putting the load on the other requests.

            var cancelOpenBids = new Action(() =>
            {
                foreach (var baseSymbol in baseSymbols)
                {
                    var openOrdersForBaseSymbol = GetOpenOrdersForTradingPairV2WithRetries(IntegrationNameRes.Coss, symbol, baseSymbol, CachePolicy.ForceRefresh);

                    var openBids = (openOrdersForBaseSymbol?.OpenOrders ?? new List<OpenOrder>())
                        .Where(queryOpenOrder => queryOpenOrder.OrderType == OrderType.Bid).ToList();

                    var openAsks = (openOrdersForBaseSymbol?.OpenOrders ?? new List<OpenOrder>())
                        .Where(queryOpenOrder => queryOpenOrder.OrderType == OrderType.Ask).ToList();

                    if (!openBids.Any()) { continue; }
                    foreach (var openOrder in openBids)
                    {
                        try
                        {
                            CancelOpenOrderWithRetries(IntegrationNameRes.Coss, openOrder.OrderId);
                        }
                        catch (Exception exception)
                        {
                            _log.Error($"Failed to cancel Coss order {openOrder.OrderId}.");
                            _log.Error(exception);
                        }
                    }
                }
            });

            cancelOpenBids();

            var cossBalance = GetCossBalanceWithRetries(symbol, CachePolicy.ForceRefresh);

            var availableSymbolBalance = cossBalance.Available;
            if (availableSymbolBalance <= 0) { return new List<AggregateOrderBookItem>(); }

            var valuationTask = LongRunningTask.Run(() =>
            {
                ethUsdResult = _workflowClient.GetUsdValue("ETH", CachePolicy.ForceRefresh);
                btcUsdResult = _workflowClient.GetUsdValue("BTC", CachePolicy.ForceRefresh);
            });

            var cossTask = LongRunningTask.Run(() =>
            {
                cossEthOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, symbol, "ETH", CachePolicy.ForceRefresh);
                cossBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, symbol, "BTC", CachePolicy.ForceRefresh);
            });

            var binanceTask = LongRunningTask.Run(() =>
            {
                binanceBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, symbol, "BTC", CachePolicy.ForceRefresh);
                binanceEthOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, symbol, "ETH", CachePolicy.ForceRefresh);
            });

            cossTask.Wait();
            valuationTask.Wait();
            binanceTask.Wait();

            if (!ethUsdResult.HasValue) { throw new ApplicationException("Failed to get ETH USD value."); }
            if (!btcUsdResult.HasValue) { throw new ApplicationException("Failed to get BTC USD value."); }

            var ethUsdValue = ethUsdResult.Value;
            var btcUsdValue = btcUsdResult.Value;

            var cossAggregateOrderBook = GenerateAggregateOrderBook(
                new List<OrderBookAndBaseSymbol>
                {
                    new OrderBookAndBaseSymbol { OrderBook = cossEthOrderBook, BaseSymbol = "ETH" },
                    new OrderBookAndBaseSymbol { OrderBook = cossBtcOrderBook, BaseSymbol = "BTC" }
                },
                new List<SymbolAndUsdValue>
                {
                    new SymbolAndUsdValue { Symbol = "ETH", UsdValue = ethUsdValue },
                    new SymbolAndUsdValue { Symbol = "BTC", UsdValue = btcUsdValue },
                    new SymbolAndUsdValue { Symbol = "USDT", UsdValue = 1 },
                });

            var binanceAggregateOrderBook = GenerateAggregateOrderBook(
                new List<OrderBookAndBaseSymbol>
                {
                    new OrderBookAndBaseSymbol { OrderBook = binanceBtcOrderBook, BaseSymbol = "ETH" },
                    new OrderBookAndBaseSymbol { OrderBook = binanceBtcOrderBook, BaseSymbol = "BTC" }
                },
                new List<SymbolAndUsdValue>
                {
                    new SymbolAndUsdValue { Symbol = "ETH", UsdValue = ethUsdValue },
                    new SymbolAndUsdValue { Symbol = "BTC", UsdValue = btcUsdValue }
                });

            var binanceAggregateBids = binanceAggregateOrderBook
                .Where(item => item.OrderType == OrderType.Bid)
                .OrderByDescending(item => item.UsdPrice)
                .ToList();

            var binanceBestBid = binanceAggregateBids.First();
            var binanceBestBidUsdPrice = binanceBestBid.UsdPrice;

            var cossAggregateBids = cossAggregateOrderBook.Where(item => item.OrderType == OrderType.Bid)
                .OrderBy(item => item.UsdPrice)
                .ToList();

            var cossViableBids = cossAggregateBids.Where(item => item.UsdPrice >= binanceBestBidUsdPrice).ToList();

            var remainingAvailable = availableSymbolBalance;
            var bidsToTake = new List<(AggregateOrderBookItem Order, decimal Quantity)>();
            foreach (var viableAsk in cossViableBids)
            {
                decimal quantityToTake;
                if (viableAsk.Quantity <= remainingAvailable)
                {
                    quantityToTake = viableAsk.Quantity;
                }
                else
                {
                    quantityToTake = remainingAvailable;
                }

                if (quantityToTake >= 0)
                {
                    bidsToTake.Add(
                    (
                        viableAsk,
                        quantityToTake
                    ));
                }

                remainingAvailable -= quantityToTake;

                if (remainingAvailable <= 0) { break; }
            }            

            foreach (var baseSymbol in baseSymbols)
            {
                var baseSymbolBidsToTake = bidsToTake.Where(item => string.Equals(item.Order.BaseSymbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase)).ToList();
                var quantityToTake = baseSymbolBidsToTake.Sum(item => item.Quantity) * RoundingErrorPrevention;
                if (quantityToTake >= availableSymbolBalance) { quantityToTake = availableSymbolBalance; }

                var worstBidToTake = baseSymbolBidsToTake.First();
                var worstSymbolPriceToTake = worstBidToTake.Order.NativePrice / RoundingErrorPrevention;

                var sellResult = _exchangeClient.SellLimit(IntegrationNameRes.Coss, symbol, baseSymbol, new QuantityAndPrice
                {
                    Quantity = quantityToTake,
                    Price = worstSymbolPriceToTake
                });

                if (!sellResult)
                {
                    throw new ApplicationException($"Failed to sell {quantityToTake} {symbol} for {quantityToTake} {baseSymbol} on {IntegrationNameRes.Coss}");
                }

                availableSymbolBalance -= quantityToTake;
            }

            cancelOpenBids();

            return cossViableBids;
        }

        public void CheckCossBase(string symbol, CachePolicy cachePolicy)
        {
            const string NormalBase = "ETH";

            var binanceOrderBookTask = LongRunningTask.Run(() => _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, symbol, NormalBase, cachePolicy));

            var omgCossBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, symbol, "COS", cachePolicy);
            var cossEthBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, "COS", NormalBase, cachePolicy);

            var cossEthBestAsk = cossEthBook.BestAsk();
            if (cossEthBestAsk == null) { return; }

            var cossEthPrice = cossEthBestAsk.Price;

            var omgCossBestAsk = omgCossBook.BestAsk();
            if (omgCossBestAsk == null) { return; }
            var omgCossPrice = omgCossBestAsk.Price;

            var effectiveOmgEthAskPrice = omgCossPrice / cossEthPrice;

            var binanceOrderBook = binanceOrderBookTask.Result;

            var binanceBestBid = binanceOrderBook.BestBid();
            if (binanceBestBid == null) { return; }
            var binanceBidPrice = binanceBestBid.Price;

            var diff = binanceBidPrice - effectiveOmgEthAskPrice;
            var diffRatio = diff / effectiveOmgEthAskPrice;
            var percentDiff = 100.0m * diffRatio;

            var resultBuilder = new StringBuilder()
                .AppendLine($"{symbol}-{NormalBase}")
                .AppendLine($"  Diff: {percentDiff.ToString("N2")} %");
                // .AppendLine($"  Coss Quantity: {cossBestAsk.Quantity.ToString("N4")}");

            _log.Debug(resultBuilder.ToString());

            /*

            buy 50 COSS @ 0.001 ETH
            + 50 COSS
            - 0.050 ETH
            (50 COSS, -0.050 ETH)

            buy 10 OMG @ 5 COSS
            + 10 OMG
            - 50 COSS
            (10 OMG, -0.050 ETH)

            OMG-ETH => 0.005 ETH
            when using the OMG-COSS pair

            */
        }

        public void AutoSellTradingPair(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            var originalOpenOrders = _exchangeClient.GetOpenOrders(IntegrationNameRes.Coss, tradingPair.Symbol, tradingPair.BaseSymbol, cachePolicy);

            var holding = _exchangeClient.GetBalance(IntegrationNameRes.Coss, tradingPair.Symbol, cachePolicy);
            // AutoSellTradingPair(tradingPair, holding, cachePolicy);

            var minimumTrade = GetMinimumTradeForBaseSymbol(tradingPair.BaseSymbol);
            if (!minimumTrade.HasValue) { return; }

            var cossOrderBookTask = LongRunningTask.Run(() => GetOrderBookWithRetries(IntegrationNameRes.Coss, tradingPair.Symbol, tradingPair.BaseSymbol, cachePolicy));
            var binanceOrderBookTask = LongRunningTask.Run(() => GetOrderBookWithRetries(IntegrationNameRes.Binance, tradingPair.Symbol, tradingPair.BaseSymbol, cachePolicy));

            var binanceOrderBook = binanceOrderBookTask.Result;
            if (binanceOrderBook == null) { return; }

            var binanceBestBid = binanceOrderBook.BestBid();
            if (binanceBestBid == null) { return; }
            var binanceBestBidPrice = binanceBestBid.Price;

            var binanceBestAsk = binanceOrderBook.BestAsk();
            if (binanceBestAsk == null) { return; }
            var binanceBestAskPrice = binanceBestAsk.Price;

            var acceptablePrice = (binanceBestBidPrice + binanceBestAskPrice) / 2.0m;

            var cossOrderBook = cossOrderBookTask.Result;
            if (cossOrderBook == null) { return; }

            if (cossOrderBook.Asks == null || !cossOrderBook.Asks.Any()) { return; }
            var viableAsks = cossOrderBook.Asks.Where(queryAsk => queryAsk.Price >= acceptablePrice)
                .ToList();

            if (!viableAsks.Any()) { return; }

            var quantityToSell = viableAsks.Sum(queryAsk => queryAsk.Quantity) / RoundingErrorPrevention;
            var priceToSell = viableAsks.Min(queryAsk => queryAsk.Price);

            var potentialSale = quantityToSell * priceToSell;
            if (potentialSale < minimumTrade.Value)
            {
                quantityToSell = (minimumTrade.Value / priceToSell) / RoundingErrorPrevention;
            }

            if (quantityToSell > holding.Available)
            {
                quantityToSell = holding.Available;
            }

            priceToSell /= RoundingErrorPrevention;

            try
            {
                _exchangeClient.SellLimit(IntegrationNameRes.Coss, tradingPair.Symbol, tradingPair.BaseSymbol, new QuantityAndPrice
                {
                    Price = priceToSell,
                    Quantity = quantityToSell
                });
            }
            finally
            {
                var openOrders = _exchangeClient.GetOpenOrders(IntegrationNameRes.Coss, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh);
                var openAsks = (openOrders ?? new List<OpenOrderForTradingPair>()).Where(queryOpenOrder => queryOpenOrder.OrderType == OrderType.Ask).ToList();
                if (openAsks.Any())
                {
                    // foreach (var openAsk in openAsks)
                    for(var i = 0; i < openAsks.Count; i++)
                    {
                        var openAsk = openAsks[i];
                        if (i > 0) { Thread.Sleep(TimeSpan.FromSeconds(1.5)); }
                        try
                        {
                            _exchangeClient.CancelOrder(IntegrationNameRes.Coss, openAsk.OrderId);
                        }
                        catch (Exception ex2)
                        {
                            _log.Error(ex2);
                        }
                    }
                }
            }
        }

        public void AutoSellTradingPair(TradingPair tradingPair, Holding balance)
        {
            var availableBalance = balance.Available;

            var cossOrderBookTask = LongRunningTask.Run(() => _exchangeClient.GetOrderBook(IntegrationNameRes.Coss, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh));
            var binanceOrderBookTask = LongRunningTask.Run(() => _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh));

            var openOrders = _exchangeClient.GetOpenOrders(IntegrationNameRes.Coss, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh);
            var openAsks = (openOrders ?? new List<OpenOrderForTradingPair>())
                .Where(item => item.OrderType == OrderType.Ask).ToList();
            var openBids = (openOrders ?? new List<OpenOrderForTradingPair>())
                .Where(item => item.OrderType == OrderType.Bid).ToList();

            var cossOrderBook = cossOrderBookTask.Result;
            if (cossOrderBook == null) { return; }

            var binanceOrderBook = binanceOrderBookTask.Result;
            if (binanceOrderBook == null) { return; }

            var cossBestBid = cossOrderBook.BestBid();
            if (cossBestBid == null) { return; }
            var cossBestBidPrice = cossBestBid.Price;

            var binanceBestBid = binanceOrderBook.BestBid();
            if (binanceBestBid == null) { return; }
            var binanceBestBidPrice = binanceBestBid.Price;

            foreach (var openAsk in openAsks)
            {
                if (openAsk.Price < binanceBestBidPrice)
                {
                    try
                    {
                        _exchangeClient.CancelOrder(IntegrationNameRes.Coss, openAsk.OrderId);
                        availableBalance += openAsk.Quantity;
                    }
                    catch (Exception exception)
                    {
                        _log.Error(exception);
                    }
                }
            }

            var bestDiff = cossBestBidPrice - binanceBestBidPrice;
            if (bestDiff < 0) { return; }

            var viableBids = new List<Order>();
            for (var i = 0; i < cossOrderBook.Bids.Count; i++)
            {
                var cossBid = cossOrderBook.Bids[i];
                var diff = cossBid.Price - binanceBestBidPrice;
                if (diff > 0)
                {
                    viableBids.Add(cossBid);
                }
            }

            if (!viableBids.Any()) { return; }
            var viableBidQuantity = viableBids.Sum(item => item.Quantity);
            var lowestViableBidPrice = viableBids.Min(item => item.Price);

            var quantityToSell = viableBidQuantity;
            if (quantityToSell > availableBalance) { quantityToSell = availableBalance; }
            var priceToSell = lowestViableBidPrice / RoundingErrorPrevention;

            var potentialSale = quantityToSell * priceToSell;
            var minimumTrade = GetMinimumTradeForBaseSymbol(tradingPair.BaseSymbol) ?? 0;
            if (potentialSale < minimumTrade)
            {
                quantityToSell = (minimumTrade / priceToSell) * RoundingErrorPrevention;
                if (quantityToSell > availableBalance) { return; }
            }

            var sellResult = _exchangeClient.SellLimit(IntegrationNameRes.Coss, tradingPair.Symbol, tradingPair.BaseSymbol,
                new QuantityAndPrice
                {
                    Price = cossBestBid.Price,
                    Quantity = quantityToSell
                });

            if (!sellResult)
            {
                _log.Error($"Failed to sell {quantityToSell} {tradingPair.Symbol} for {priceToSell} {tradingPair.BaseSymbol}");
            }
        }

        private static Random Random = new Random();

        public void AutoTusdWithReverseBinanceSymbol(string binanceSymbol)
        {
            var symbol = binanceSymbol;
            const string BaseSymbol = "TUSD";
            var maxBidQuantity = BaseSymbolOpenQuantityDictionary[binanceSymbol];

            // TODO: Need to sort this out by value, not just whether or not it's ETH or BTC.
            var maxAskQuantity = string.Equals(binanceSymbol, "BTC", StringComparison.InvariantCultureIgnoreCase)
                ? 0.01m
                : 0.25m;

            const decimal MinimumProfitPercentDiff = 1.5m;

            const decimal DefaultPriceTick = 0.00000001m;

            var cossAgentConfig = _configClient.GetCossAgentConfig();
            if (!cossAgentConfig.IsCossAutoTradingEnabled)
            {
                _log.Info("CossArbUtil.AutoTusdWithReverseBinanceSymbol() - Auto trading is disabed. Aborting.");
                return;
            }

            var cossTradingPairs = _exchangeClient.GetTradingPairs(IntegrationNameRes.Coss, CachePolicy.AllowCache);
            var cossTradingPair = cossTradingPairs.Single(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase) && string.Equals(item.BaseSymbol, BaseSymbol, StringComparison.InvariantCultureIgnoreCase));
            var priceTick = cossTradingPair.PriceTick ?? DefaultPriceTick;
            var lotSize = cossTradingPair.LotSize ?? DefaultLotSize;

            var balances = GetBalancesWithRetries(IntegrationNameRes.Coss, CachePolicy.ForceRefresh) ?? new HoldingInfo();
            var binanceSymbolAvailable = balances.GetAvailableForSymbol(binanceSymbol);
            var tusdAvailable = balances.GetAvailableForSymbol(BaseSymbol);

            var openOrders = _exchangeClient.GetOpenOrdersForTradingPairV2(IntegrationNameRes.Coss, binanceSymbol, "TUSD", CachePolicy.ForceRefresh);
            if (openOrders?.OpenOrders != null && openOrders.OpenOrders.Any())
            {
                foreach (var openOrder in openOrders.OpenOrders)
                {
                    CancelOpenOrderWithRetries(IntegrationNameRes.Coss, openOrder.OrderId);
                }
            }

            OrderBook binanceTusdOrderBook = null;
            OrderBook cossOrderBook = null;

            var binanceTask = LongRunningTask.Run(() =>
            {
                binanceTusdOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, "TUSD", binanceSymbol, CachePolicy.AllowCache);
            });

            var cossTask = LongRunningTask.Run(() =>
            {
                cossOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Coss, binanceSymbol, "TUSD", CachePolicy.AllowCache);
            });

            binanceTask.Wait();
            cossTask.Wait();

            var binanceOrderBook = InvertOrderBook(binanceTusdOrderBook);

            var binanceBestBid = binanceOrderBook.BestBid();
            var binanceBestBidPrice = binanceBestBid.Price;

            var cossBestBid = cossOrderBook.BestBid();
            var cossBestBidPrice = cossBestBid.Price;

            var anticipatedBidPrice = cossBestBidPrice + priceTick;

            var bidDiff = binanceBestBidPrice - anticipatedBidPrice;
            var bidDiffRatio = bidDiff / anticipatedBidPrice;
            var bidPercentDiff = 100.0m * bidDiffRatio;

            Console.WriteLine($"Bid {bidPercentDiff.ToString("N4")} % diff");

            if (bidPercentDiff >= MinimumProfitPercentDiff)
            {
                var price = anticipatedBidPrice;

                var maxPotentialBidQuantity = tusdAvailable / price / 1.01m;

                var untruncatedQuantity = maxBidQuantity > maxPotentialBidQuantity
                    ? maxPotentialBidQuantity
                    : maxBidQuantity;

                var quantity = MathUtil.ConstrainToMultipleOf(untruncatedQuantity, lotSize);

                try
                {
                    _log.Info($"About to place a bid on Coss for {quantity} {symbol} at {price} {BaseSymbol}.");
                    var orderResult = _exchangeClient.BuyLimit(IntegrationNameRes.Coss, binanceSymbol, "TUSD", new QuantityAndPrice
                    {
                        Quantity = quantity,
                        Price = price
                    });

                    if (orderResult)
                    {
                        _log.Info($"Successfully placed a bid on Coss for {quantity} {symbol} at {price} {BaseSymbol}.");
                    }
                    else
                    {
                        _log.Error($"Failed to place a bid on Coss for {quantity} {symbol} at {price} {BaseSymbol}.");
                    }
                }
                catch(Exception exception)
                {
                    _log.Error($"Failed to place a bid on Coss for {quantity} {symbol} at {price} {BaseSymbol}.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                }
            }

            var binanceBestAsk = binanceOrderBook.BestAsk();
            var cossBestAsk = cossOrderBook.BestAsk();

            var binanceBestAskPrice = binanceBestAsk.Price;            
            var cossBestAskPrice = cossBestAsk.Price;

            var anticipatedAskPrice = cossBestAskPrice - priceTick;

            var askDiff = anticipatedAskPrice - binanceBestAskPrice;
            var askDiffRatio = askDiff / anticipatedAskPrice;
            var askPercentDiff = 100.0m * askDiffRatio;

            if (askPercentDiff >= MinimumProfitPercentDiff)
            {
                var maxPotentialQuantityToSell = binanceSymbolAvailable / 1.1m;
                var quantity = MathUtil.ConstrainToMultipleOf(
                    maxAskQuantity > maxPotentialQuantityToSell
                        ? maxPotentialQuantityToSell
                        : maxAskQuantity,
                    lotSize);

                // var quantity = 50;
                var price = anticipatedAskPrice;

                try
                {
                    _log.Info($"About to place an ask on Coss for {quantity} {symbol} at {price} {BaseSymbol}.");
                    var orderResult = _exchangeClient.SellLimit(IntegrationNameRes.Coss, binanceSymbol, "TUSD", new QuantityAndPrice
                    {
                        Quantity = quantity,
                        Price = price
                    });

                    if (orderResult)
                    {
                        _log.Info($"Successfully placed an ask on Coss for {quantity} {symbol} at {price} {BaseSymbol}.");
                    }
                    else
                    {
                        _log.Error($"Failed to place an ask on Coss for {quantity} {symbol} at {price} {BaseSymbol}.");
                    }
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to place an ask on Coss for {quantity} {symbol} at {price} {BaseSymbol}.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                }
            }
        }

        private OrderBook InvertOrderBook(OrderBook originalOrderBook)
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

        public void AutoEthBtc()
        {
            const string Symbol = "ETH";
            const string BaseSymbol = "BTC";

            var cossAgentConfig = _configClient.GetCossAgentConfig();
            if (cossAgentConfig == null) { throw new ApplicationException("Failed to retrieve coss agent config."); }
            if (!cossAgentConfig.IsCossAutoTradingEnabled)
            {
                _log.Info("Coss auto trading is disabled. Not running the coss eth-btc workflow.");
                return;
            }

            var openOrders = _exchangeClient.GetOpenOrders(IntegrationNameRes.Coss, "ETH", "BTC", CachePolicy.ForceRefresh);
            if (openOrders != null && openOrders.Any())
            {
                foreach (var openOrder in openOrders)
                {
                    _log.Debug($"Cancelling order for {openOrder.Quantity} {openOrder.Symbol} at {openOrder.Price} {openOrder.BaseSymbol}");
                    _exchangeClient.CancelOrder(IntegrationNameRes.Coss, openOrder.OrderId);
                }
            }

            var binanceOrderBookTask = LongRunningTask.Run(() => _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, "ETH", "BTC", CachePolicy.ForceRefresh));
            var cossOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Coss, "ETH", "BTC", CachePolicy.ForceRefresh);
            var binanceOrderBook = binanceOrderBookTask.Result;

            var binanceBestAsk = binanceOrderBook.BestAsk();
            var binanceBestAskPrice = binanceBestAsk.Price;

            var binanceBestBid = binanceOrderBook.BestBid();
            var binanceBestBidPrice = binanceBestBid.Price;

            var cossBestAsk = cossOrderBook.BestAsk();
            var cossBestAskPrice = cossBestAsk.Price;

            var cossBestBid = cossOrderBook.BestBid();
            var cossBestBidPrice = cossBestBid.Price;
            
            const decimal MinimumBtcTargetPercentDiff = 0.75m;
            const decimal DefaultEthBtcTargetPercentDiff = 1.2m;

            var targetPercentDiff =
                cossAgentConfig != null && cossAgentConfig.EthThreshold >= MinimumBtcTargetPercentDiff
                ? cossAgentConfig.EthThreshold
                : DefaultEthBtcTargetPercentDiff;

            var orderedCossAsks = cossOrderBook.Asks.OrderBy(item => item.Price).ToList();

            decimal quantityToBuy = 0;
            decimal highestPriceToBuy = 0;
            foreach (var cossAsk in orderedCossAsks)
            {
                var priceDiff = binanceBestBidPrice - cossAsk.Price;
                var priceDiffRatio = priceDiff / cossAsk.Price;
                var priceDiffPercentage = 100.0m * priceDiffRatio;
                if (priceDiffPercentage >= targetPercentDiff)
                {
                    quantityToBuy += cossAsk.Quantity;
                    if (cossAsk.Price > highestPriceToBuy)
                    {
                        highestPriceToBuy = cossAsk.Price;
                    }
                }
            }

            var minBtc = GetMinimumTradeForBaseSymbol("BTC");
            var minEth = GetMinimumTradeForBaseSymbol("ETH");

            if (quantityToBuy > 0)
            {
                var effectivePrice = highestPriceToBuy * RoundingErrorPrevention;
                var potentialBtcTraded = effectivePrice * quantityToBuy * RoundingErrorPrevention;

                decimal effectiveQuantity;
                if (potentialBtcTraded < minBtc)
                {
                    effectiveQuantity = highestPriceToBuy / minBtc.Value;
                }
                else
                {
                    effectiveQuantity = quantityToBuy;
                }

                var buyResult = _exchangeClient.BuyLimit(IntegrationNameRes.Coss, "ETH", "BTC",
                    new QuantityAndPrice
                    {
                        Quantity = effectiveQuantity,
                        Price = effectivePrice
                    });

                if (!buyResult)
                {
                    _log.Error($"Failed to purchase {effectiveQuantity} {Symbol} at {effectivePrice} {BaseSymbol}");
                }

                var openOrdersAfterPurchase = _exchangeClient.GetOpenOrders(IntegrationNameRes.Coss, "ETH", "BTC", CachePolicy.ForceRefresh);
                var openBidsAfterPurchase = (openOrdersAfterPurchase ?? new List<OpenOrderForTradingPair>())
                    .Where(queryOpenOrder => queryOpenOrder.OrderType == OrderType.Bid)
                    .ToList();

                foreach (var openOrder in openBidsAfterPurchase)
                {
                    try
                    {
                        CancelOpenOrderWithRetries(IntegrationNameRes.Coss, openOrder.OrderId);
                    }
                    catch (Exception exception)
                    {
                        _log.Error(exception);
                    }
                }
            }

            decimal quantityToSell = 0;
            decimal lowestPriceToSell = 99999999999999;

            var orderedCossBids = cossOrderBook.Bids.OrderByDescending(item => item.Price).ToList();
            foreach (var cossBid in orderedCossBids)
            {
                var priceDiff = cossBid.Price - binanceBestAskPrice;
                var priceDiffRatio = priceDiff / binanceBestAskPrice;
                var priceDiffPercentage = 100.0m * priceDiffRatio;
                if (priceDiffPercentage >= targetPercentDiff)
                {
                    quantityToSell += cossBid.Quantity;
                    if (cossBid.Price < lowestPriceToSell)
                    {
                        lowestPriceToSell = cossBid.Price;
                    }
                }
            }

            if (quantityToSell > 0)
            {
                var effectivePrice = lowestPriceToSell / RoundingErrorPrevention;
                var potentialTrade = quantityToSell * effectivePrice;
                decimal effectiveQuantity;
                if (potentialTrade < minEth.Value)
                {
                    effectiveQuantity = minEth.Value / effectivePrice;
                }
                else
                {
                    effectiveQuantity = quantityToSell;
                }

                var sellResult = _exchangeClient.SellLimit(IntegrationNameRes.Coss, "ETH", "BTC", new QuantityAndPrice
                {
                    Quantity = effectiveQuantity,
                    Price = effectivePrice
                });

                if (!sellResult)
                {
                    _log.Error($"Failed to sell {effectiveQuantity} {Symbol} at {effectivePrice} {BaseSymbol}");
                }

                var openOrdersAfterSale = _exchangeClient.GetOpenOrders(IntegrationNameRes.Coss, "ETH", "BTC", CachePolicy.ForceRefresh);
                var openAsksAfterSale = (openOrdersAfterSale ?? new List<OpenOrderForTradingPair>())
                    .Where(queryOpenOrder => queryOpenOrder.OrderType == OrderType.Ask)
                    .ToList();

                foreach (var openOrder in openAsksAfterSale)
                {
                    try
                    {
                        CancelOpenOrderWithRetries(IntegrationNameRes.Coss, openOrder.OrderId);
                    }
                    catch(Exception exception)
                    {
                        _log.Error(exception);
                    }
                }
            }
        }

        public void AutoEthBtcV2()
        {
            const decimal MinimumPermittedBid = 0.001m;
            const decimal NormalEthBtcTradeQuantity = 0.25m;
            const decimal LowEndEthBtcTradeQuantity = 0.10m;

            const string Symbol = "ETH";
            const string BaseSymbol = "BTC";

            OrderBook binanceEthBtcOrderBook = null;
            var binanceTask = LongRunningTask.Run(() =>
            {
                binanceEthBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, Symbol, BaseSymbol, CachePolicy.ForceRefresh);
            });

            var cossTradingPairs = _exchangeClient.GetTradingPairs(IntegrationNameRes.Coss, CachePolicy.AllowCache);
            var ethBtcTradingPair = cossTradingPairs.SingleOrDefault(item => string.Equals(item.Symbol, "ETH", StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.BaseSymbol, "BTC", StringComparison.InvariantCultureIgnoreCase));

            var LotSize = ethBtcTradingPair.LotSize.Value;
            if (LotSize <= 0) { throw new ArgumentOutOfRangeException("LotSize"); }

            var PriceTick = ethBtcTradingPair.PriceTick.Value;
            if (PriceTick <= 0) { throw new ArgumentOutOfRangeException("PriceTick"); }

            var openOrders = GetOpenOrdersWithRetries(Symbol, BaseSymbol, CachePolicy.ForceRefresh) ?? new List<OpenOrderForTradingPair>();

            var cossBalances = GetCossBalancesWithRetries(CachePolicy.ForceRefresh);
            var ethBalance = cossBalances.GetHoldingForSymbol("ETH");
            var ethTotal = ethBalance?.Total ?? 0;
            var ethAvailable = ethBalance?.Available ?? 0;

            var btcBalance = cossBalances?.GetHoldingForSymbol("BTC");
            var btcTotal = btcBalance?.Total ?? 0;
            var btcAvailable = btcBalance?.Available ?? 0;

            var idealPercentDiffToPlaceAsk = 2.0m;
            var minPercentDiffToPlaceAsk = 1.5m;
            var minPercentDiffToRetainAsk = 1.2m;
            if (ethTotal >= 5.0m && btcTotal <= 0.02m)
            {
                minPercentDiffToPlaceAsk = 0.5m;
                minPercentDiffToRetainAsk = 0.25m;
            }
            else if (ethTotal >= 2.0m && btcTotal <= 0.01m)
            {
                minPercentDiffToPlaceAsk = 0.75m;
                minPercentDiffToRetainAsk = 0.5m;
            }

            var idealPercentDiffToPlaceBid = 3.5m;
            var minPercentDiffToPlaceBid = 1.5m;

            // todo: continue to work on the bid logic.

            var cossEthBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, Symbol, BaseSymbol, CachePolicy.ForceRefresh);

            var cossEthBtcBestAskPrice = cossEthBtcOrderBook.BestAsk().Price;
            if (cossEthBtcBestAskPrice <= 0)
            {
                CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, "ETH", "BTC");
                throw new ApplicationException($"Coss's best {Symbol}-{BaseSymbol} ask price should be > 0.");
            }

            var cossEthBtcBestBidPrice = cossEthBtcOrderBook.BestBid().Price;
            if (cossEthBtcBestBidPrice <= 0)
            {
                CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, "ETH", "BTC");
                throw new ApplicationException($"Coss's best {Symbol}-{BaseSymbol} bid price should be > 0.");
            }

            if (cossEthBtcBestBidPrice >= cossEthBtcBestAskPrice)
            {
                CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, "ETH", "BTC");
                throw new ApplicationException($"Coss's {Symbol}-{BaseSymbol} best bid price should not be >= than its best ask price.");
            }

            binanceTask.Wait();

            var binanceEthBtcBestAskPrice = binanceEthBtcOrderBook.BestAsk().Price;
            if (binanceEthBtcBestAskPrice <= 0)
            {
                CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, "ETH", "BTC");
                throw new ApplicationException($"Binance's best {Symbol}-{BaseSymbol} ask price should be > 0.");
            }

            var binanceEthBtcBestBidPrice = binanceEthBtcOrderBook.BestBid().Price;
            if (binanceEthBtcBestBidPrice <= 0)
            {
                CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, "ETH", "BTC");
                throw new ApplicationException($"Binance's best {Symbol}-{BaseSymbol} bid price should be > 0.");
            }

            if (binanceEthBtcBestBidPrice >= binanceEthBtcBestAskPrice)
            {
                CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, "ETH", "BTC");
                throw new ApplicationException($"Binance's best {Symbol}-{BaseSymbol} bid price should not be >= its best ask price.");
            }

            // TODO: Add volume weights.
            var binanceEthBtcRatio = new List<decimal> { binanceEthBtcBestAskPrice, binanceEthBtcBestBidPrice }.Average();

            OpenOrderForTradingPair retainedOpenAsk = null;

            var shouldReloadOrderBook = false;
            var openAsks = openOrders.Where(item => item.OrderType == OrderType.Ask).ToList();
            if (openAsks.Count >= 2)
            {
                foreach(var openOrder in openAsks)
                {
                    CancelOpenOrderWithRetries(IntegrationNameRes.Coss, openOrder);
                    var match = cossEthBtcOrderBook.Asks.FirstOrDefault(item => item.Price == openOrder.Price && item.Quantity == openOrder.Quantity);
                    if (match != null)
                    {
                        cossEthBtcOrderBook.Asks.Remove(match);
                    }
                    else
                    {
                        shouldReloadOrderBook = true;
                    }
                }
                
                ethAvailable += openAsks.Sum(item => item.Quantity);
            }
            else if (openAsks.Count == 1)
            {
                var openAsk = openAsks.Single();
                if (openAsk.Price != cossEthBtcBestAskPrice)
                {
                    CancelOpenOrderWithRetries(IntegrationNameRes.Coss, openAsk);
                    ethAvailable += openAsk.Quantity;

                    var match = cossEthBtcOrderBook.Asks.FirstOrDefault(item => item.Price == openAsk.Price && item.Quantity == openAsk.Quantity);
                    if (match != null)
                    {
                        cossEthBtcOrderBook.Asks.Remove(match);
                    }
                    else
                    {
                        shouldReloadOrderBook = true;
                    }
                }
                else
                {
                    var diff = openAsk.Price - binanceEthBtcBestAskPrice;
                    var ratio = diff / binanceEthBtcBestAskPrice;
                    var percentDiff = 100.0m * ratio;

                    var quantityToRetainAsk = NormalEthBtcTradeQuantity * 0.75m;

                    if (percentDiff < minPercentDiffToRetainAsk || openAsk.Quantity < quantityToRetainAsk)
                    {
                        CancelOpenOrderWithRetries(IntegrationNameRes.Coss, openAsk);
                        ethAvailable += openAsk.Quantity;

                        var match = cossEthBtcOrderBook.Asks.FirstOrDefault(item => item.Price == openAsk.Price && item.Quantity == openAsk.Quantity);
                        if (match != null)
                        {
                            cossEthBtcOrderBook.Asks.Remove(match);
                        }
                        else
                        {
                            shouldReloadOrderBook = true;
                        }
                    }
                    else
                    {
                        retainedOpenAsk = openAsk;
                    }
                }
            }

            var openBids = openOrders.Where(item => item.OrderType == OrderType.Bid).ToList();
            foreach (var openOrder in openBids)
            {
                CancelOpenOrderWithRetries(IntegrationNameRes.Coss, openOrder);
                btcAvailable += openOrder.Price * openOrder.Quantity;

                var match = cossEthBtcOrderBook.Bids.FirstOrDefault(item => item.Price == openOrder.Price && item.Quantity == openOrder.Quantity);
                if (match != null)
                {
                    cossEthBtcOrderBook.Bids.Remove(match);
                }
                else
                {
                    shouldReloadOrderBook = true;
                }
            }

            if (shouldReloadOrderBook)
            {
                cossEthBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, Symbol, BaseSymbol, CachePolicy.ForceRefresh);

                cossEthBtcBestAskPrice = cossEthBtcOrderBook.BestAsk().Price;
                if (cossEthBtcBestAskPrice <= 0) { throw new ApplicationException($"Coss's best {Symbol}-{BaseSymbol} ask price should be > 0."); }

                cossEthBtcBestBidPrice = cossEthBtcOrderBook.BestBid().Price;
                if (cossEthBtcBestBidPrice <= 0) { throw new ApplicationException($"Coss's best {Symbol}-{BaseSymbol} bid price should be > 0."); }

                if (cossEthBtcBestBidPrice >= cossEthBtcBestAskPrice)
                {
                    CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, "ETH", "BTC");
                    throw new ApplicationException($"Coss's {Symbol}-{BaseSymbol} best bid price should not be >= than its best ask price.");
                }
            }

            decimal? priceToBid = null;
            var idealBidPrice = MathUtil.ConstrainToMultipleOf(binanceEthBtcBestBidPrice * (100.0m - idealPercentDiffToPlaceBid) / 100.0m, PriceTick);
            if (idealBidPrice > cossEthBtcBestBidPrice)
            {
                priceToBid = idealBidPrice;
            }
            else
            {
                var tickUpBidPrice = cossEthBtcBestBidPrice + PriceTick;
                var diff = binanceEthBtcBestBidPrice - tickUpBidPrice;
                var ratio = diff / binanceEthBtcBestBidPrice;
                var percentDif = 100.0m * ratio;

                if (percentDif > minPercentDiffToPlaceBid)
                {
                    priceToBid = tickUpBidPrice;
                }
            }

            decimal? quantityToBid = null;
            if (priceToBid.HasValue)
            {
                var maxPotentialBid = btcAvailable / priceToBid.Value / 1.01m;
                if (NormalEthBtcTradeQuantity > maxPotentialBid)
                {
                    if (maxPotentialBid >= MinimumPermittedBid)
                    {
                        quantityToBid = MathUtil.ConstrainToMultipleOf(maxPotentialBid, LotSize);
                    }
                }
                else
                {
                    quantityToBid = MathUtil.ConstrainToMultipleOf(NormalEthBtcTradeQuantity, LotSize);
                }
            }

            if (quantityToBid.HasValue && priceToBid.HasValue)
            {
                var quantity = quantityToBid.Value;
                var price = priceToBid.Value;

                _log.Info($"About to place a bid on Coss for {quantity} {Symbol} at {price} {BaseSymbol}.");
                try
                {
                    var orderResult = BuyLimitWithRetryOnNoResponse(IntegrationNameRes.Coss, Symbol, BaseSymbol, new QuantityAndPrice
                    {
                        Price = price,
                        Quantity = quantity
                    });

                    if (orderResult)
                    {
                        _log.Info($"Successfully placed a bid on Coss for {quantity} {Symbol} at {price} {BaseSymbol}.");
                    }
                    else
                    {
                        _log.Error($"Failed to place an bid on Coss for {quantity} {Symbol} at {price} {BaseSymbol}.");
                    }
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to place an bid on Coss for {quantity} {Symbol} at {price} {BaseSymbol}.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                }
            }

            if (retainedOpenAsk == null)
            {
                decimal? askPriceToPlace = null;
                var idealAskPrice = MathUtil.ConstrainToMultipleOf(binanceEthBtcBestAskPrice * (100.0m + idealPercentDiffToPlaceAsk) / 100.0m, PriceTick);
                if (idealAskPrice < cossEthBtcBestAskPrice)
                {
                    askPriceToPlace = idealAskPrice;
                }
                else
                {
                    var tickDownAskPrice = cossEthBtcBestAskPrice - PriceTick;
                    var diff = tickDownAskPrice - binanceEthBtcBestAskPrice;
                    var ratio = diff / binanceEthBtcBestAskPrice;
                    var percentDiff = 100.0m * ratio;
                    if (percentDiff >= minPercentDiffToPlaceAsk)
                    {
                        askPriceToPlace = tickDownAskPrice;
                    }
                }

                decimal? quantityToAsk = null;
                if (ethTotal >= 6m && ethAvailable >= 1.5m && btcTotal <= 0.15m && ethAvailable > 2.01m * NormalEthBtcTradeQuantity)
                {
                    quantityToAsk = 2.0m * NormalEthBtcTradeQuantity;
                }
                else if (ethTotal >= 2.0m && ethAvailable >= 1.25m * NormalEthBtcTradeQuantity)
                {
                    quantityToAsk = NormalEthBtcTradeQuantity;
                }
                else if (ethAvailable >= 1.25m * LowEndEthBtcTradeQuantity)
                {
                    quantityToAsk = LowEndEthBtcTradeQuantity;
                }
                
                if (askPriceToPlace.HasValue && quantityToAsk.HasValue)
                {
                    var quantity = quantityToAsk.Value;
                    var price = askPriceToPlace.Value;

                    _log.Info($"About to place an ask on Coss for {quantity} {Symbol} at {price} {BaseSymbol}.");
                    try
                    {
                        var orderResult = SellLimitWithRetryOnNoResponse(IntegrationNameRes.Coss, Symbol, BaseSymbol, new QuantityAndPrice
                        {
                            Price = price,
                            Quantity = quantity
                        });

                        if (orderResult)
                        {
                            _log.Info($"Successfully placed an ask on Coss for {quantity} {Symbol} at {price} {BaseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place an ask on Coss for {quantity} {Symbol} at {price} {BaseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place an ask on Coss for {quantity} {Symbol} at {price} {BaseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
            }
        }

        public void AutoEthGusd()
        {
            AutoEthXusd("GUSD");
        }

        public void AutoBtcGusd()
        {
            AutoBtcXusd("GUSD");
        }

        public void AutoBtcUsdc()
        {
            AutoBtcXusd("USDC");
        }

        public void AutoBtcUsdt()
        {
            AutoBtcXusd("USDT");
        }

        public void AutoEthUsdc()
        {
            AutoEthXusd("USDC");
        }

        public void AutoEthUsdt()
        {
            AutoEthXusd("USDT");
        }

        private void AutoEthXusdOld(string dollarSymbol)
        {
            // 0.12341231
            const decimal PriceTick = 0.00000001m;
            const int PriceDecimals = 8;
            const decimal LotSize = 0.00000001m;

            const string Symbol = "ETH";
            string baseSymbol = dollarSymbol;

            decimal? ethValuationResponse = null;
            var valuationTask = LongRunningTask.Run(() =>
            {
                ethValuationResponse = _workflowClient.GetUsdValue("ETH", CachePolicy.ForceRefresh);
            });

            CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, Symbol, baseSymbol);

            var cossEthGusdOrderbook = GetOrderBookWithRetries(IntegrationNameRes.Coss, Symbol, baseSymbol, CachePolicy.ForceRefresh);
            var cossEthGusdBestBidPrice = cossEthGusdOrderbook.BestBid().Price;
            if (cossEthGusdBestBidPrice <= 0) { throw new ApplicationException($"Coss's {Symbol}-{baseSymbol} best bid price should be > 0."); }

            var cossEthGusdBestAskPrice = cossEthGusdOrderbook.BestAsk().Price;
            if (cossEthGusdBestAskPrice <= 0) { throw new ApplicationException($"Coss's {Symbol}-{baseSymbol} best ask price should be > 0."); }

            if (cossEthGusdBestBidPrice >= cossEthGusdBestAskPrice)
            {
                throw new ApplicationException($"Coss's {Symbol}-{baseSymbol} best ask price should be greater than its best bid price.");
            }

            valuationTask.Wait();

            var ethValue = ethValuationResponse.Value;
            if (ethValue <= 0) { throw new ApplicationException("ETH's value should be > 0."); }

            // Bid
            var bidPriceToPlace = MathUtil.Truncate(ethValue * 0.85m, PriceDecimals);
            var quantityToBid = MathUtil.ConstrainToMultipleOf(
                string.Equals(dollarSymbol, "USDC", StringComparison.InvariantCultureIgnoreCase)
                // ? 0.015m
                // ? 0.030m
                ? 1.5m
                : 1.0m
                , LotSize);

            {
                var price = bidPriceToPlace;
                var quantity = quantityToBid;

                _log.Info($"About to place a bid on Coss for {quantity} {Symbol} at {price} {baseSymbol}.");
                try
                {
                    var orderResult = _exchangeClient.BuyLimit(IntegrationNameRes.Coss, Symbol, baseSymbol, new QuantityAndPrice
                    {
                        Price = price,
                        Quantity = quantity
                    });

                    if (orderResult)
                    {
                        _log.Info($"Successfully placed a bid on Coss for {quantity} {Symbol} at {price} {baseSymbol}.");
                    }
                    else
                    {
                        _log.Error($"Failed to place a bid on Coss for {quantity} {Symbol} at {price} {baseSymbol}.");
                    }
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to place a bid on Coss for {quantity} {Symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                }
            }

            var askPriceToPlace = MathUtil.Truncate(ethValue * 1.15m, PriceDecimals);
            var quantityToAsk = MathUtil.ConstrainToMultipleOf(0.25m, LotSize);
            {
                var price = askPriceToPlace;
                var quantity = quantityToAsk;

                _log.Info($"About to place an ask on Coss for {quantity} {Symbol} at {price} {baseSymbol}.");
                try
                {
                    var orderResult = _exchangeClient.SellLimit(IntegrationNameRes.Coss, Symbol, baseSymbol, new QuantityAndPrice
                    {
                        Price = price,
                        Quantity = quantity
                    });

                    if (orderResult)
                    {
                        _log.Info($"Successfully placed a ask on Coss for {quantity} {Symbol} at {price} {baseSymbol}.");
                    }
                    else
                    {
                        _log.Error($"Failed to place an ask on Coss for {quantity} {Symbol} at {price} {baseSymbol}.");
                    }
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to place an ask on Coss for {quantity} {Symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                }
            }
        }

        private void AutoEthXusd(string dollarSymbol)
        {
            AutoXusd(dollarSymbol, "ETH");
        }
        
        private void AutoBtcXusd(string dollarSymbol)
        {
            AutoXusd(dollarSymbol, "BTC");
        }

        private void AutoXusd(string dollarSymbol, string cryptoSymbol)
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

            var cossTradingPairs = _exchangeClient.GetTradingPairs(IntegrationNameRes.Coss, CachePolicy.AllowCache);
            var tradingPair = cossTradingPairs.Single(item => string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.BaseSymbol, dollarSymbol, StringComparison.InvariantCultureIgnoreCase));

            if (!tradingPair.LotSize.HasValue) { throw new ApplicationException($"Coss's {symbol}-{baseSymbol} trading pair does not have a lot size configured."); }
            var lotSize = tradingPair.LotSize.Value;

            if (!tradingPair.PriceTick.HasValue) { throw new ApplicationException($"Coss's {symbol}-{baseSymbol} trading pair does not have a price tick configured."); }
            var priceTick = tradingPair.PriceTick.Value;

            var cossBalances = _exchangeClient.GetBalances(IntegrationNameRes.Coss, CachePolicy.ForceRefresh);
            var symbolAvailable = cossBalances?.GetAvailableForSymbol(symbol) ?? 0;
            var xusdAvailable = cossBalances?.GetAvailableForSymbol(dollarSymbol) ?? 0;

            CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, symbol, baseSymbol);

            var cossSymbolXusdOrderbook = GetOrderBookWithRetries(IntegrationNameRes.Coss, symbol, baseSymbol, CachePolicy.ForceRefresh);
            var cossSymbolXusdBestBid =  cossSymbolXusdOrderbook.BestBid();
            var cossSymbolXusdBestBidPrice = cossSymbolXusdBestBid?.Price ?? 0.0001m;
            if (cossSymbolXusdBestBidPrice <= 0) { throw new ApplicationException($"Coss's {symbol}-{baseSymbol} best bid price should be > 0."); }

            var cossSymbolXusdBestAsk = cossSymbolXusdOrderbook.BestAsk();
            var cossSymbolXusdBestAskPrice = cossSymbolXusdBestAsk?.Price ?? 9999999999999.0m;
            if (cossSymbolXusdBestAskPrice <= 0) { throw new ApplicationException($"Coss's {symbol}-{baseSymbol} best ask price should be > 0."); }

            if (cossSymbolXusdBestBidPrice >= cossSymbolXusdBestAskPrice)
            {
                throw new ApplicationException($"Coss's {symbol}-{baseSymbol} best ask price should be greater than its best bid price.");
            }

            valuationTask.Wait();

            var symbolValue = symbolValuationResponse.Value;
            if (symbolValue <= 0) { throw new ApplicationException($"{symbol}'s value should be > 0."); }

            decimal? bidPriceToPlace = null;

            // Bid
            var idealBidPrice = MathUtil.ConstrainToMultipleOf(symbolValue * (100.0m - IdealBidPercentDiff) / 100.0m, priceTick);
            if (idealBidPrice > cossSymbolXusdBestBidPrice)
            {
                bidPriceToPlace = idealBidPrice;
            }
            else
            {
                var upTickBidPrice = cossSymbolXusdBestBidPrice + priceTick;
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

                    _log.Info($"About to place a bid on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                    try
                    {
                        var orderResult = _exchangeClient.BuyLimitV2(IntegrationNameRes.Coss, symbol, baseSymbol, new QuantityAndPrice
                        {
                            Price = price,
                            Quantity = quantity
                        });

                        if (orderResult.WasSuccessful)
                        {
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

            decimal? askPriceToPlace = null;
            var idealAskPrice = MathUtil.ConstrainToMultipleOf(symbolValue * (100.0m + IdealAskPercentDiff) / 100.0m, priceTick);
            if (idealAskPrice < cossSymbolXusdBestAskPrice)
            {
                askPriceToPlace = idealAskPrice;
            }
            else
            {
                var downTickAskPrice = cossSymbolXusdBestAskPrice - priceTick;
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

                    _log.Info($"About to place an ask on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
                    try
                    {
                        var orderResult = _exchangeClient.SellLimitV2(IntegrationNameRes.Coss, symbol, baseSymbol, new QuantityAndPrice
                        {
                            Price = price,
                            Quantity = quantity
                        });

                        if (orderResult.WasSuccessful)
                        {
                            _log.Info($"Successfully placed a ask on Coss for {quantity} {symbol} at {price} {baseSymbol}.");
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

        public class AggregateOrderBookItem
        {
            public OrderType OrderType { get; set; }
            public string BaseSymbol { get; set; }
            public decimal NativePrice { get; set; }
            public decimal UsdPrice { get; set; }
            public decimal Quantity { get; set; }
        }

        private class OrderBookAndBaseSymbol
        {
            public OrderBook OrderBook { get; set; }
            public string BaseSymbol { get; set; }
        }

        private class SymbolAndUsdValue
        {
            public string Symbol { get; set; }
            public decimal UsdValue { get; set; }
        }

        private List<AggregateOrderBookItem> GenerateAggregateOrderBook(
            List<OrderBookAndBaseSymbol> orderBooks,
            List<SymbolAndUsdValue> valuations,
            OrderBook cossVsBtcOrderBook = null)
        {
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
                if (string.Equals(symbolOrderBook.BaseSymbol, "COS", StringComparison.InvariantCultureIgnoreCase))
                {
                    // TODO: really should be taking into account both the COSS-ETH and COSS-BTC order books
                    if (cossVsBtcOrderBook != null)
                    {
                        foreach (var order in symbolOrderBook.Orders ?? new List<Order>())
                        {
                            if (symbolOrderBook.OrderType == OrderType.Bid)
                            {
                                var cossVsBtcBestBid = cossVsBtcOrderBook.BestBid();
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
                                var cossVsBtcBestAsk = cossVsBtcOrderBook.BestAsk();
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
                    var baseSymbolUsdValue = valuations.SingleOrDefault(queryValution =>
                        string.Equals(
                            queryValution.Symbol, 
                            symbolOrderBook.BaseSymbol, 
                            StringComparison.InvariantCultureIgnoreCase)
                    );

                    if (baseSymbolUsdValue != null)
                    {

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
            }

            aggregateOrderBook = aggregateOrderBook != null
                ? aggregateOrderBook.OrderBy(item => item.UsdPrice).ToList()
                : null;
            
            return aggregateOrderBook;
        }

        public void AcquireArkCoss()
        {
            const string Symbol = "ARK";
            const string BaseSymbol = "COS";

            const decimal MinStep = 0.00000001m;
            const decimal MinProfitPercentage = 5.0m;
            const decimal CossMinSaleQuantity = 1.0m;
            const decimal CossMaxSaleQuantity = 1000.0m;
            const decimal OptimalDiffRatio = 0.075m;
            const decimal OptimalRatio = 1.0m - OptimalDiffRatio;
            const decimal MinArkSaleQuantity = 0.001m;

            const decimal LotSize = 0.00000001m;
            const int PriceDecimals = 7;

            OrderBook binanceArkBtcOrderBook = null;
            var binanceArkBtcOrderBookTask = LongRunningTask.Run(() =>
            {
                binanceArkBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, "ARK", "BTC", CachePolicy.ForceRefresh);
            });

            CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, "ARK", "COS");

            var cossBalances = GetBalancesWithRetries(IntegrationNameRes.Coss, CachePolicy.ForceRefresh);
            var arkAvailable = cossBalances.GetAvailableForSymbol("ARK");
            var cossHolding = cossBalances.GetHoldingForSymbol("COS");
            var cossAvailable = cossBalances.GetAvailableForSymbol("COS");
            
            var cossArkCossOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, "ARK", "COS", CachePolicy.ForceRefresh);
            var cossCossBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, "COS", "BTC", CachePolicy.ForceRefresh);

            var cossArkCossBestBidPrice = cossArkCossOrderBook.BestBid().Price;
            if (cossArkCossBestBidPrice <= 0) { throw new ApplicationException("Coss's ARK-COSS best bid price should be > 0."); }

            binanceArkBtcOrderBookTask.Wait();
            var binanceArkBtcBestBidPrice = binanceArkBtcOrderBook.BestBid().Price;
            if (binanceArkBtcBestBidPrice <= 0) { throw new ApplicationException("Binance's ARK-BTC best bid price should be > 0."); }

            var cossCossBtcBestBidPrice = cossCossBtcOrderBook.BestBid().Price;
            if (cossCossBtcBestBidPrice <= 0) { throw new ApplicationException("Coss's COSS-BTC best bid price should be > 0."); }

            var cossCossBtcBestAskPrice = cossCossBtcOrderBook.BestAsk().Price;
            if (cossCossBtcBestAskPrice <= 0) { throw new ApplicationException("Coss's COSS-BTC best ask price should be > 0."); }

            var inferredBinanceArkCossBestBidPrice = binanceArkBtcBestBidPrice / cossCossBtcBestBidPrice;

            decimal potentialBidPrice;
            var optimalBidPrice = MathUtil.Truncate(inferredBinanceArkCossBestBidPrice * OptimalRatio, PriceDecimals);
            if (optimalBidPrice > cossArkCossBestBidPrice)
            {
                potentialBidPrice = optimalBidPrice;
            }
            else
            {
                potentialBidPrice = cossArkCossBestBidPrice + MinStep;
            }

            var profitDiff = inferredBinanceArkCossBestBidPrice - potentialBidPrice;
            var profitRatio = profitDiff / potentialBidPrice;
            var profitPercentDiff = 100.0m * profitRatio;

            if (profitPercentDiff >= MinProfitPercentage)
            {
                if (cossAvailable >= CossMinSaleQuantity)
                {
                    var price = potentialBidPrice;
                    var cossQuantity = cossAvailable < CossMaxSaleQuantity ? cossAvailable : CossMaxSaleQuantity;
                    var quantity = MathUtil.ConstrainToMultipleOf(cossQuantity / price, LotSize);
                    _log.Info($"About to place a bid on Coss for {quantity} {Symbol} at {price} {BaseSymbol}.");

                    try
                    {
                        var orderResult = _exchangeClient.BuyLimit(IntegrationNameRes.Coss, "ARK", "COS", new QuantityAndPrice
                        {
                            Quantity = quantity,
                            Price = price
                        });

                        if (orderResult)
                        {
                            _log.Info($"Successfully placed a bid on Coss for {quantity} {Symbol} at {price} {BaseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place a bid on Coss for {quantity} {Symbol} at {price} {BaseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place a bid on Coss for {quantity} {Symbol} at {price} {BaseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
            }

            bool justTestingThis = true;
            if (arkAvailable >= MinArkSaleQuantity || justTestingThis)
            {
                var cossArkCossBestAskPrice = cossArkCossOrderBook.BestAsk().Price;
                var cossArkCossBestAskPriceAsBtc = cossArkCossBestAskPrice * binanceArkBtcBestBidPrice;

                var binanceArkBtcBestAskPrice = binanceArkBtcOrderBook.BestAsk().Price;
                // var nominalArkBtcAskPrice = binanceArkBtcBestAskPrice * 0.85m;
                var nominalArkCossAskPrice = cossArkCossBestAskPrice * 0.85m;
                var nominalArkCossAskPriceAsBtc = nominalArkCossAskPrice * cossCossBtcBestBidPrice;
                var diff = nominalArkCossAskPriceAsBtc - binanceArkBtcBestAskPrice;
                var diffRatio = diff / binanceArkBtcBestAskPrice;
                var percentDiff = 100.0m * diffRatio;

                if (percentDiff >= MinProfitPercentage)
                {
                    // _exchangeClient.SellLimit(something...);
                }
            }
        }

        public class CossValuationData
        {
            public OrderBook BinanceEthBtcOrderBook { get; set; }
            public OrderBook CossCossBtcOrderBook { get; set; }
            public OrderBook CossCossEthOrderBook { get; set; }
        }

        public decimal DetermineCossBtcValue(CossValuationData valuationData)
        {
            if (valuationData == null) { throw new ArgumentNullException(nameof(valuationData)); }

            OrderBook binanceEthBtcOrderBook = valuationData.BinanceEthBtcOrderBook;

            var cossCossBtcOrderBook = valuationData.CossCossBtcOrderBook;
            var cossCossEthOrderBook = valuationData.CossCossEthOrderBook;

            var binanceEthBtcBestAskPrice = binanceEthBtcOrderBook.BestAsk().Price;
            var binanceEthBtcBestBidPrice = binanceEthBtcOrderBook.BestBid().Price;
            
            var ethBtcRatio = new List<decimal> { binanceEthBtcBestAskPrice, binanceEthBtcBestBidPrice }.Average();

            decimal totalWeight = 0;
            decimal totalTop = 0;

            var btcAsks = cossCossBtcOrderBook.Asks.OrderBy(item => item.Price).Take(3).ToList();
            for (var i = 0; i < btcAsks.Count; i++)
            {
                var order = btcAsks[i];
                var quantitySqrt = (decimal)Math.Sqrt((double)order.Quantity);
                var weight = quantitySqrt / ((i + 1) * (i + 1));

                totalTop += order.Price * weight;
                totalWeight += weight;
            }
            
            var btcBids = cossCossBtcOrderBook.Bids.OrderByDescending(item => item.Price).Take(3).ToList();
            for (var i = 0; i < btcBids.Count; i++)
            {
                var order = btcBids[i];
                var quantitySqrt = (decimal)Math.Sqrt((double)order.Quantity);
                var weight = quantitySqrt / ((i + 1) * (i + 1));

                totalTop += order.Price * weight;
                totalWeight += weight;
            }

            var ethAsks = cossCossEthOrderBook.Asks.OrderBy(item => item.Price).Take(3).ToList();
            for (var i = 0; i < ethAsks.Count; i++)
            {
                var order = ethAsks[i];
                var quantitySqrt = (decimal)Math.Sqrt((double)order.Quantity);
                var weight = quantitySqrt / ((i + 1) * (i + 1));

                totalTop += order.Price * weight * ethBtcRatio;
                totalWeight += weight;
            }

            var ethBids = cossCossEthOrderBook.Bids.OrderByDescending(item => item.Price).Take(3).ToList();
            for (var i = 0; i < ethBids.Count; i++)
            {
                var order = ethBids[i];
                var quantitySqrt = (decimal)Math.Sqrt((double)order.Quantity);
                var weight = quantitySqrt / ((i + 1) * (i + 1));

                totalTop += order.Price * weight * ethBtcRatio;
                totalWeight += weight;
            }

            var valuation = totalTop / totalWeight;

            return valuation;
        }

        public void AcquireXdce()
        {
            const string AcquisitionSymbol = "XDCE";
            const decimal MinXdceEthDiff = 0.00000001m;
            const decimal MinXdceBtcDiff = 0.00000001m;
            const decimal XdceLotSize = 100.0m;
            const decimal MinBidProfitPercetDiff = 1.0m;

            const decimal XdceBidQuantity = 200.0m;

            // var ethValuationResponse = _workflowClient.GetUsdValueV2("ETH", CachePolicy.ForceRefresh);
            // var btcValuationResponse = _workflowClient.GetUsdValueV2("BTC", CachePolicy.ForceRefresh);

            OrderBook binanceEthBtcOrderBook = null;
            var binanceTask = LongRunningTask.Run(() =>
            {
                binanceEthBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, "ETH", "BTC", CachePolicy.ForceRefresh);
            });

            CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, AcquisitionSymbol, "ETH");
            CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, AcquisitionSymbol, "BTC");

            var cossXdceEthOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, AcquisitionSymbol, "ETH", CachePolicy.ForceRefresh);
            var cossXdceBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, AcquisitionSymbol, "BTC", CachePolicy.ForceRefresh);

            var cossBalances = GetBalancesWithRetries(IntegrationNameRes.Coss, CachePolicy.ForceRefresh);
            var cossEthAvailable = cossBalances.GetAvailableForSymbol("ETH");
            var cossBtcAvailable = cossBalances.GetAvailableForSymbol("BTC");
            var cossXdceAvailable = cossBalances.GetAvailableForSymbol(AcquisitionSymbol);

            binanceTask.Wait();

            var binanceEthBtcBestBidPrice = binanceEthBtcOrderBook.BestBid().Price;
            if (binanceEthBtcBestBidPrice <= 0) { throw new ApplicationException("Binance's ETH-BTC best bid price should be > 0."); }

            var binanceEthBtcBestAskPrice = binanceEthBtcOrderBook.BestAsk().Price;
            if (binanceEthBtcBestAskPrice <= 0) { throw new ApplicationException("Binance's ETH-BTC best ask price should be > 0."); }

            var averageBinanceEthBtcPrice = new List<decimal> { binanceEthBtcBestBidPrice, binanceEthBtcBestAskPrice }.Average();
            decimal ethBtcRatio = averageBinanceEthBtcPrice;

            var cossXdceEthBestBidPrice = cossXdceEthOrderBook.BestBid().Price;
            if (cossXdceEthBestBidPrice <= 0) { throw new ApplicationException("Coss's XDCE-ETH best bid price should be > 0."); }

            var cossXdceEthBestAskPrice = cossXdceEthOrderBook.BestAsk().Price;
            if (cossXdceEthBestAskPrice <= 0) { throw new ApplicationException("Coss's XDCE-ETH best ask price should be > 0."); }

            var cossXdceBtcBestBidPrice = cossXdceBtcOrderBook.BestBid().Price;
            if (cossXdceBtcBestBidPrice <= 0) { throw new ApplicationException("Coss's XDCE-BTC best bid price should be > 0."); }

            var cossXdceBtcBestAskPrice = cossXdceBtcOrderBook.BestAsk().Price;
            if (cossXdceBtcBestAskPrice <= 0) { throw new ApplicationException("Coss's XDCE-BTC best ask price should be > 0."); }

            var averageCossXdceEthPrice = new List<decimal> { cossXdceEthBestBidPrice, cossXdceEthBestAskPrice }.Average();
            var avergaeCossXdceBtcPrice = new List<decimal> { cossXdceBtcBestBidPrice, cossXdceBtcBestAskPrice }.Average();

            var cossXdceEthPotentialAskPrice = cossXdceEthBestAskPrice - MinXdceEthDiff;
            var cossXdceBtcPotentialAskPrice = cossXdceBtcBestAskPrice - MinXdceBtcDiff;
            var cossXdceEthPotentialAskPriceAsBtc = cossXdceEthPotentialAskPrice * ethBtcRatio;

            var quantityToSell = MathUtil.ConstrainToMultipleOf(cossXdceAvailable, XdceLotSize);

            if (quantityToSell > 0)
            {
                if (cossXdceEthPotentialAskPriceAsBtc <= cossXdceBtcPotentialAskPrice)
                {
                    var price = cossXdceEthPotentialAskPrice;
                    var quantity = quantityToSell;
                    var baseSymbol = "ETH";

                    try
                    {
                        _log.Info($"About to place a limit ask on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        var orderResult = _exchangeClient.SellLimit(IntegrationNameRes.Coss, AcquisitionSymbol, baseSymbol, new QuantityAndPrice
                        {
                            Quantity = quantity,
                            Price = price
                        });
                        if (orderResult)
                        {
                            _log.Info($"Successfully placed a limit ask on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place a limit ask on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place a limit ask on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
                else
                {
                    var price = cossXdceBtcPotentialAskPrice;
                    var quantity = quantityToSell;
                    var baseSymbol = "BTC";

                    try
                    {
                        _log.Info($"About to place a limit ask on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        var orderResult = _exchangeClient.SellLimit(IntegrationNameRes.Coss, AcquisitionSymbol, baseSymbol, new QuantityAndPrice
                        {
                            Quantity = quantity,
                            Price = price
                        });
                        if (orderResult)
                        {
                            _log.Info($"Successfully placed a limit ask on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place a limit ask on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place a limit ask on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
            }

            #region "Regions are terrible. Clean this up."
            var cossXdceEthPotentialBidPrice = cossXdceEthBestBidPrice + MinXdceEthDiff;
            var cossXdceBtcPotentialBidPrice = cossXdceBtcBestBidPrice + MinXdceBtcDiff;

            var cossXdceEthPotentialBidPriceAsBtc = cossXdceEthPotentialBidPrice * ethBtcRatio;
            if (cossXdceEthPotentialBidPriceAsBtc <= cossXdceBtcPotentialBidPrice)
            {
                var price = cossXdceEthPotentialBidPrice;
                var priceDiff = cossXdceEthBestAskPrice - price;
                var priceRatio = priceDiff / price;
                var percentDiff = 100.0m * priceRatio;

                if (percentDiff >= MinBidProfitPercetDiff)
                {
                    var baseSymbol = "ETH";
                    var quantity = XdceBidQuantity;

                    try
                    {
                        _log.Info($"About to place a limit bid on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        var orderResult = _exchangeClient.BuyLimit(IntegrationNameRes.Coss, AcquisitionSymbol, baseSymbol, new QuantityAndPrice
                        {
                            Price = price,
                            Quantity = quantity
                        });

                        if (orderResult)
                        {
                            _log.Info($"Successfully placed a limit bid on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place a limit bid on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place a limit bid on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
            }
            else
            {
                var price = cossXdceBtcPotentialBidPrice;
                var priceDiff = cossXdceBtcBestAskPrice - price;
                var priceRatio = priceDiff / price;
                var percentDiff = 100.0m * priceRatio;

                if (percentDiff >= MinBidProfitPercetDiff)
                {
                    var baseSymbol = "BTC";
                    var quantity = XdceBidQuantity;

                    try
                    {
                        _log.Info($"About to place a limit bid on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        var orderResult = _exchangeClient.BuyLimit(IntegrationNameRes.Coss, AcquisitionSymbol, baseSymbol, new QuantityAndPrice
                        {
                            Price = price,
                            Quantity = quantity
                        });

                        if (orderResult)
                        {
                            _log.Info($"Successfully placed a limit bid on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place a limit bid on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place a limit bid on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
            }
            #endregion
        }

        public void AcquireBwt()
        {
            const string AcquisitionSymbol = "BWT";
            const decimal MinAcquisitionEthDiff = 0.00000001m;
            const decimal MinAcquisitionBtcDiff = 0.00000001m;
            const decimal AcquisitionLotSize = 0.00000001m;
            const decimal MinBidProfitPercetDiff = 1.0m;

            const decimal AcquisitionBidQuantity = 100.0m;

            OrderBook binanceEthBtcOrderBook = null;
            var binanceTask = LongRunningTask.Run(() =>
            {
                binanceEthBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, "ETH", "BTC", CachePolicy.ForceRefresh);
            });

            CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, AcquisitionSymbol, "ETH");
            CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, AcquisitionSymbol, "BTC");

            var cossAcqEthOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, AcquisitionSymbol, "ETH", CachePolicy.ForceRefresh);
            var cossAcqBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, AcquisitionSymbol, "BTC", CachePolicy.ForceRefresh);

            var cossBalances = GetBalancesWithRetries(IntegrationNameRes.Coss, CachePolicy.ForceRefresh);
            var cossEthAvailable = cossBalances.GetAvailableForSymbol("ETH");
            var cossBtcAvailable = cossBalances.GetAvailableForSymbol("BTC");
            var cossAcqAvailable = cossBalances.GetAvailableForSymbol(AcquisitionSymbol);

            binanceTask.Wait();

            var binanceEthBtcBestBidPrice = binanceEthBtcOrderBook.BestBid().Price;
            if (binanceEthBtcBestBidPrice <= 0) { throw new ApplicationException("Binance's ETH-BTC best bid price should be > 0."); }

            var binanceEthBtcBestAskPrice = binanceEthBtcOrderBook.BestAsk().Price;
            if (binanceEthBtcBestAskPrice <= 0) { throw new ApplicationException("Binance's ETH-BTC best ask price should be > 0."); }

            var averageBinanceEthBtcPrice = new List<decimal> { binanceEthBtcBestBidPrice, binanceEthBtcBestAskPrice }.Average();
            var ethBtcRatio = averageBinanceEthBtcPrice;

            var cossAcqEthBestBidPrice = cossAcqEthOrderBook.BestBid().Price;
            if (cossAcqEthBestBidPrice <= 0) { throw new ApplicationException("Coss's XDCE-ETH best bid price should be > 0."); }

            var cossAcqEthBestAskPrice = cossAcqEthOrderBook.BestAsk().Price;
            if (cossAcqEthBestAskPrice <= 0) { throw new ApplicationException("Coss's XDCE-ETH best ask price should be > 0."); }

            var cossAcqBtcBestBidPrice = cossAcqBtcOrderBook.BestBid().Price;
            if (cossAcqBtcBestBidPrice <= 0) { throw new ApplicationException("Coss's XDCE-BTC best bid price should be > 0."); }

            var cossAcqBtcBestAskPrice = cossAcqBtcOrderBook.BestAsk().Price;
            if (cossAcqBtcBestAskPrice <= 0) { throw new ApplicationException("Coss's XDCE-BTC best ask price should be > 0."); }

            var averageCossAcqEthPrice = new List<decimal> { cossAcqEthBestBidPrice, cossAcqEthBestAskPrice }.Average();
            var avergaeCossAcqBtcPrice = new List<decimal> { cossAcqBtcBestBidPrice, cossAcqBtcBestAskPrice }.Average();

            var cossAcqEthPotentialAskPrice = cossAcqEthBestAskPrice - MinAcquisitionEthDiff;
            var cossAcqBtcPotentialAskPrice = cossAcqBtcBestAskPrice - MinAcquisitionBtcDiff;
            var cossAcqEthPotentialAskPriceAsBtc = cossAcqEthPotentialAskPrice * ethBtcRatio;

            var quantityToSell = MathUtil.ConstrainToMultipleOf(cossAcqAvailable, AcquisitionLotSize);

            if (quantityToSell > 0)
            {
                if (cossAcqEthPotentialAskPriceAsBtc <= cossAcqBtcPotentialAskPrice)
                {
                    var price = cossAcqEthPotentialAskPrice;
                    var quantity = quantityToSell;
                    var baseSymbol = "ETH";

                    try
                    {
                        _log.Info($"About to place a limit ask on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        var orderResult = _exchangeClient.SellLimit(IntegrationNameRes.Coss, AcquisitionSymbol, baseSymbol, new QuantityAndPrice
                        {
                            Quantity = quantity,
                            Price = price
                        });
                        if (orderResult)
                        {
                            _log.Info($"Successfully placed a limit ask on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place a limit ask on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place a limit ask on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
                else
                {
                    var price = cossAcqBtcPotentialAskPrice;
                    var quantity = quantityToSell;
                    var baseSymbol = "BTC";

                    try
                    {
                        _log.Info($"About to place a limit ask on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        var orderResult = _exchangeClient.SellLimit(IntegrationNameRes.Coss, AcquisitionSymbol, baseSymbol, new QuantityAndPrice
                        {
                            Quantity = quantity,
                            Price = price
                        });
                        if (orderResult)
                        {
                            _log.Info($"Successfully placed a limit ask on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place a limit ask on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place a limit ask on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
            }

            #region "Regions are terrible. Clean this up."
            var cossAcqEthPotentialBidPrice = cossAcqEthBestBidPrice + MinAcquisitionEthDiff;
            var cossAcqBtcPotentialBidPrice = cossAcqBtcBestBidPrice + MinAcquisitionBtcDiff;

            var cossAcqEthPotentialBidPriceAsBtc = cossAcqEthPotentialBidPrice * ethBtcRatio;
            if (cossAcqEthPotentialBidPriceAsBtc <= cossAcqBtcPotentialBidPrice)
            {
                var price = cossAcqEthPotentialBidPrice;
                var priceDiff = cossAcqEthBestAskPrice - price;
                var priceRatio = priceDiff / price;
                var percentDiff = 100.0m * priceRatio;

                if (percentDiff >= MinBidProfitPercetDiff)
                {
                    var baseSymbol = "ETH";
                    var quantity = AcquisitionBidQuantity;

                    try
                    {
                        _log.Info($"About to place a limit bid on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        var orderResult = _exchangeClient.BuyLimit(IntegrationNameRes.Coss, AcquisitionSymbol, baseSymbol, new QuantityAndPrice
                        {
                            Price = price,
                            Quantity = quantity
                        });

                        if (orderResult)
                        {
                            _log.Info($"Successfully placed a limit bid on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place a limit bid on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place a limit bid on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
            }
            else
            {
                var price = cossAcqBtcPotentialBidPrice;
                var priceDiff = cossAcqBtcBestAskPrice - price;
                var priceRatio = priceDiff / price;
                var percentDiff = 100.0m * priceRatio;

                if (percentDiff >= MinBidProfitPercetDiff)
                {
                    var baseSymbol = "BTC";
                    var quantity = AcquisitionBidQuantity;

                    try
                    {
                        _log.Info($"About to place a limit bid on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        var orderResult = _exchangeClient.BuyLimit(IntegrationNameRes.Coss, AcquisitionSymbol, baseSymbol, new QuantityAndPrice
                        {
                            Price = price,
                            Quantity = quantity
                        });

                        if (orderResult)
                        {
                            _log.Info($"Successfully placed a limit bid on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place a limit bid on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place a limit bid on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
            }
            #endregion
        }

        public void AcquireXdceTusd()
        {
            const string Symbol = "XDCE";

            const decimal quantity = 10000.0m;

            const int PriceDecimals = 8;
            const decimal IdealPercentDiff = 20.0m;

            OrderBook binanceTusdBtcOrderBook = null;
            OrderBook binanceTusdEthOrderBook = null;

            var binanceTask = LongRunningTask.Run(() =>
            {
                binanceTusdBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, "TUSD", "BTC", CachePolicy.ForceRefresh);
                binanceTusdEthOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, "TUSD", "ETH", CachePolicy.ForceRefresh);
            });

            var cossXdceTusdOpenOrders = GetOpenOrdersWithRetries(Symbol, "TUSD", CachePolicy.ForceRefresh) ?? new List<OpenOrderForTradingPair>();

            var cossBalances = GetBalancesWithRetries(IntegrationNameRes.Coss, CachePolicy.ForceRefresh);

            var cossXdceBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, Symbol, "BTC", CachePolicy.ForceRefresh);

            // 0.0000034
            var cossXdceBtcBestBidPrice = cossXdceBtcOrderBook.BestBid().Price;
            if (cossXdceBtcBestBidPrice <= 0) { throw new ApplicationException($"Coss's best {Symbol}-BTC bid price should be > 0."); }

            var cossXdceBtcBestAskPrice = cossXdceBtcOrderBook.BestAsk().Price;
            if (cossXdceBtcBestAskPrice <= 0) { throw new ApplicationException($"Coss's best {Symbol}-BTC ask price should be > 0."); }

            if (cossXdceBtcBestBidPrice >= cossXdceBtcBestAskPrice) { throw new ApplicationException($"Coss's best {Symbol}-BTC bid price should be less than its best ask price."); }

            var cossXdceEthOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, Symbol, "ETH", CachePolicy.ForceRefresh);
            var cossXdceEthBestBidPrice = cossXdceEthOrderBook.BestBid().Price;
            if (cossXdceEthBestBidPrice <= 0) { throw new ApplicationException($"Coss's best {Symbol}-ETH bid price should be > 0."); }

            var cossXdceEthBestAskPrice = cossXdceEthOrderBook.BestAsk().Price;
            if (cossXdceEthBestAskPrice <= 0) { throw new ApplicationException($"Coss's best {Symbol}-ETH ask price should be > 0."); }

            if (cossXdceEthBestBidPrice >= cossXdceEthBestAskPrice) { throw new ApplicationException("Coss's best XDCE-ETH bid price should be less than its best ask price."); }

            var cossXdceTusdOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, Symbol, "TUSD", CachePolicy.ForceRefresh);
            var cossXdceTusdBestBidPrice = cossXdceTusdOrderBook.BestBid().Price;
            if (cossXdceTusdBestBidPrice <= 0) { throw new ApplicationException($"Coss's best {Symbol}-TUSD bid price should be > 0."); }

            var cossXdceTusdBestAskPrice = cossXdceTusdOrderBook.BestAsk().Price;
            if (cossXdceTusdBestAskPrice <= 0) { throw new ApplicationException($"Coss's best {Symbol}-TUSD ask price should be > 0."); }
            if (cossXdceTusdBestBidPrice >= cossXdceTusdBestAskPrice) { throw new ApplicationException($"Coss's best {Symbol}-TUSD bid should be less than its best ask."); }

            binanceTask.Wait();

            // 0.00022333
            var binanceTusdBtcBestBidPrice = binanceTusdBtcOrderBook.BestBid().Price;
            if (binanceTusdBtcBestBidPrice <= 0) { throw new ApplicationException("Binances best TUSD-BTC bid price should be > 0."); }

            var binanceTusdBtcBestAskPrice = binanceTusdBtcOrderBook.BestAsk().Price;
            if (binanceTusdBtcBestAskPrice <= 0) { throw new ApplicationException("Binances best TUSD-BTC ask price should be > 0."); }

            if (binanceTusdBtcBestBidPrice >= binanceTusdBtcBestAskPrice) { throw new ApplicationException("Binances best TUSD-BTC bid price should be less than its best ask price."); }

            var binanceTusdEthBestBidPrice = binanceTusdEthOrderBook.BestBid().Price;
            if (binanceTusdEthBestBidPrice <= 0) { throw new ApplicationException("Binances best TUSD-ETH bid price should be > 0."); }

            var binanceTusdEthBestAskPrice = binanceTusdEthOrderBook.BestAsk().Price;
            if (binanceTusdEthBestAskPrice <= 0) { throw new ApplicationException("Binances best TUSD-ETH ask price should be > 0."); }

            if (binanceTusdEthBestBidPrice >= binanceTusdEthBestAskPrice) { throw new ApplicationException("Binances best TUSD-ETH bid price should be less than its best ask price."); }

            var cossXdceTusdOpenBids = cossXdceTusdOpenOrders.Where(item => item.OrderType == OrderType.Bid).ToList();
            var shouldCancelAllOpenBids = false;
            OpenOrder existingXdceTusdOpenBid = null;
            if (cossXdceTusdOpenBids.Count >= 2)
            {
                shouldCancelAllOpenBids = true;
            }
            else if (cossXdceTusdOpenBids.Count == 1)
            {
                existingXdceTusdOpenBid = cossXdceTusdOpenBids.Single();
                if (existingXdceTusdOpenBid.Price > cossXdceTusdBestBidPrice)
                {
                    shouldCancelAllOpenBids = true;
                }
                else
                {
                    // remove our open bid from the order book so that we can recalculate the bid that we'd like to place.
                    var match = cossXdceTusdOrderBook.Bids.FirstOrDefault(item => item.Price == existingXdceTusdOpenBid.Price && item.Quantity == existingXdceTusdOpenBid.Quantity);
                    if (match != null)
                    {
                        cossXdceTusdOrderBook.Bids.Remove(match);
                    }
                    else
                    {
                        shouldCancelAllOpenBids = true;
                    }
                }
            }

            var shouldReloadCossXdceTusdOrderBook = false;
            if (shouldCancelAllOpenBids)
            {
                foreach (var openOrder in cossXdceTusdOpenBids)
                {
                    var match = cossXdceTusdOrderBook.Bids.FirstOrDefault(item => item.Price == openOrder.Price && item.Quantity == openOrder.Quantity);
                    if (match != null)
                    {
                        cossXdceTusdOrderBook.Bids.Remove(match);
                    }
                    else
                    {
                        shouldReloadCossXdceTusdOrderBook = true;
                    }

                    CancelOpenOrderWithRetries(IntegrationNameRes.Coss, openOrder);
                }
            }

            if (shouldReloadCossXdceTusdOrderBook)
            {
                cossXdceTusdOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, Symbol, "TUSD", CachePolicy.ForceRefresh);
            }

            // check for the best bid and best ask again.
            // this must be done if we cancelled any orders or if the order book was reloaded.
            cossXdceTusdBestBidPrice = cossXdceTusdOrderBook.BestBid().Price;
            if (cossXdceTusdBestBidPrice <= 0) { throw new ApplicationException($"Coss's best {Symbol}-TUSD bid price should be > 0."); }

            cossXdceTusdBestAskPrice = cossXdceTusdOrderBook.BestAsk().Price;
            if (cossXdceTusdBestAskPrice <= 0) { throw new ApplicationException($"Coss's best {Symbol}-TUSD ask price should be > 0."); }
            if (cossXdceTusdBestBidPrice >= cossXdceTusdBestAskPrice) { throw new ApplicationException("Coss's best XDCE-TUSD bid should be less than its best ask."); }

            // 0.0000034 / 0.00022333 = 0.015224107822504815
            var cossXdceBtcBestBidPriceAsXdceTusd = cossXdceBtcBestBidPrice / binanceTusdBtcBestBidPrice;
            var cossXdceEthBestBidPriceAsXdceTusd = cossXdceEthBestBidPrice / binanceTusdEthBestBidPrice;

            var idealXdceTusdBidPriceFromBtc = MathUtil.Truncate(cossXdceBtcBestBidPriceAsXdceTusd * (100.0m - IdealPercentDiff) / 100.0m, PriceDecimals);
            var idealXdceTusdBidPriceFromEth = MathUtil.Truncate(cossXdceEthBestBidPriceAsXdceTusd * (100.0m - IdealPercentDiff) / 100.0m, PriceDecimals);

            var idealXdceTusdBidPrice = idealXdceTusdBidPriceFromBtc < idealXdceTusdBidPriceFromEth ? idealXdceTusdBidPriceFromBtc : idealXdceTusdBidPriceFromEth;

            var shouldKeepExistingBid = false;
            if (existingXdceTusdOpenBid != null)
            {
                var priceDiff = existingXdceTusdOpenBid.Price - idealXdceTusdBidPrice;
                var priceRatio = priceDiff / idealXdceTusdBidPrice;
                var pricePercentDiff = 100.0m * priceRatio;
                var absPricePercentDiff = Math.Abs(pricePercentDiff);

                var quantityDiff = existingXdceTusdOpenBid.Quantity - quantity;
                var quantityRatio = quantityDiff / existingXdceTusdOpenBid.Quantity;
                var quantityPercentDiff = 100.0m * quantityRatio;
                var absQuantityPercentDiff = Math.Abs(quantityPercentDiff);

                if (absPricePercentDiff < 2.0m && absQuantityPercentDiff < 10.0m)
                {
                    shouldKeepExistingBid = true;
                }
            }

            if (!shouldKeepExistingBid && existingXdceTusdOpenBid != null)
            {
                CancelOpenOrderWithRetries(IntegrationNameRes.Coss, existingXdceTusdOpenBid);
                // todo: credit the in-memory tusd balance.
            }

            if (!shouldKeepExistingBid && idealXdceTusdBidPrice > cossXdceTusdBestBidPrice)
            {
                var price = idealXdceTusdBidPrice;

                var baseSymbol = "TUSD";

                _log.Info($"About to place a bid on {IntegrationNameRes.Coss} for {quantity} {Symbol} at {price} {baseSymbol}.");
                try
                {
                    var orderResult = BuyLimitWithRetryOnNoResponse(IntegrationNameRes.Coss, Symbol, baseSymbol, new QuantityAndPrice
                    {
                        Quantity = quantity,
                        Price = idealXdceTusdBidPriceFromBtc
                    });

                    if (orderResult)
                    {
                        _log.Info($"Successfully placed a bid on {IntegrationNameRes.Coss} for {quantity} {Symbol} at {price} {baseSymbol}.");
                    }
                    else
                    {
                        _log.Error($"Failed to place a bid on {IntegrationNameRes.Coss} for {quantity} {Symbol} at {price} {baseSymbol}.");
                    }
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to place a bid on {IntegrationNameRes.Coss} for {quantity} {Symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                }
            }
        }

        public void AcquireBwtGusd()
        {
            const int PriceDecimals = 8;
            const decimal LotSize = 0.00000001m;

            const string Symbol = "BWT";
            const string BaseSymbol = "GUSD";
            const decimal BidQuantity = 10000;

            var ethValue = _workflowClient.GetUsdValue("ETH", CachePolicy.AllowCache);
            if (!ethValue.HasValue) { throw new ApplicationException("Failed to retrieve ETH value."); }
            var holdings = GetBalancesWithRetries(IntegrationNameRes.Coss, CachePolicy.ForceRefresh);

            var cossBwtEthOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, Symbol, "ETH", CachePolicy.ForceRefresh);

            var bestBwtEthAskPrice = cossBwtEthOrderBook.BestAsk().Price;
            if (bestBwtEthAskPrice <= 0) { throw new ApplicationException($"Best {Symbol}-ETH ask price should be > 0."); }

            var bestBwtEthBidPrice = cossBwtEthOrderBook.BestBid().Price;
            if (bestBwtEthBidPrice <= 0) { throw new ApplicationException($"Best {Symbol}-ETH bid price should be > 0."); }

            if (bestBwtEthBidPrice >= bestBwtEthAskPrice) { throw new ApplicationException($"The best {Symbol}-ETH bid price should not be >= the best ask price."); }

            var bestBwtEthBidPriceAsUsd = bestBwtEthBidPrice * ethValue.Value;
            var optimalBwtGusdBidPrice = MathUtil.Truncate(bestBwtEthBidPriceAsUsd * 0.75m, PriceDecimals);

            var openOrders = GetOpenOrdersWithRetries(Symbol, BaseSymbol, CachePolicy.ForceRefresh) ?? new List<OpenOrderForTradingPair>();
            var openBids = openOrders.Where(item => item.OrderType == OrderType.Bid).ToList();
            if (openBids.Count >= 2)
            {
                foreach(var openBid in openBids)
                {
                    CancelOpenOrderWithRetries(IntegrationNameRes.Coss, openBid);
                }
            }
            else if (openBids.Count == 1)
            {
                var openBid = openBids.Single();
                if (MathUtil.IsWithinPercentDiff(openBid.Price, optimalBwtGusdBidPrice, 5) && MathUtil.IsWithinPercentDiff(openBid.Quantity, BidQuantity, 10))
                {
                    _log.Info($"Keeping our bid on {Symbol}-{BaseSymbol}");
                    return;
                }

                CancelOpenOrderWithRetries(IntegrationNameRes.Coss, openBid);
            }

            var cossBwtGusdOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, Symbol, BaseSymbol, CachePolicy.ForceRefresh);

            var price = optimalBwtGusdBidPrice;
            var quantity = BidQuantity;

            try
            {
                _log.Info($"About to place a bid on Coss for {quantity} {Symbol} at {price} {BaseSymbol}.");
                var orderResult = _exchangeClient.BuyLimit(IntegrationNameRes.Coss, Symbol, BaseSymbol, new QuantityAndPrice
                {
                    Price = price,
                    Quantity = quantity
                });

                if (orderResult)
                {
                    _log.Info($"Successfully placed a bid on Coss for {quantity} {Symbol} at {price} {BaseSymbol}.");
                }
                else
                {
                    _log.Error($"Failed to place a bid on Coss for {quantity} {Symbol} at {price} {BaseSymbol}.");
                }
            }
            catch (Exception exception)
            {
                _log.Error($"Failed to place a bid on Coss for {quantity} {Symbol} at {price} {BaseSymbol}.{Environment.NewLine}{exception.Message}");
                _log.Error(exception);
            }
        }

        /// <summary>
        /// Places bids on BWT-TUSD
        /// </summary>
        public void AcquireBwtTusd()
        {
            const decimal quantity = 10000.0m;

            const int PriceDecimals = 8;
            const decimal IdealPercentDiff = 20.0m;

            OrderBook binanceTusdBtcOrderBook = null;
            OrderBook binanceTusdEthOrderBook = null;

            var binanceTask = LongRunningTask.Run(() =>
            {
                binanceTusdBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, "TUSD", "BTC", CachePolicy.ForceRefresh);
                binanceTusdEthOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, "TUSD", "ETH", CachePolicy.ForceRefresh);
            });

            var cossBwtTusdOpenOrders = GetOpenOrdersWithRetries("BWT", "TUSD", CachePolicy.ForceRefresh) ?? new List<OpenOrderForTradingPair>();

            var cossBalances = GetBalancesWithRetries(IntegrationNameRes.Coss, CachePolicy.ForceRefresh);

            var cossBwtBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, "BWT", "BTC", CachePolicy.ForceRefresh);
            
            // 0.0000034
            var cossBwtBtcBestBidPrice = cossBwtBtcOrderBook.BestBid().Price;
            if (cossBwtBtcBestBidPrice <= 0) { throw new ApplicationException("Coss's best BWT-BTC bid price should be > 0."); }

            var cossBwtBtcBestAskPrice = cossBwtBtcOrderBook.BestAsk().Price;
            if (cossBwtBtcBestAskPrice <= 0) { throw new ApplicationException("Coss's best BWT-BTC ask price should be > 0."); }

            if (cossBwtBtcBestBidPrice >= cossBwtBtcBestAskPrice) { throw new ApplicationException("Coss's best BWT-BTC bid price should be less than its best ask price."); }

            var cossBwtEthOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, "BWT", "ETH", CachePolicy.ForceRefresh);
            var cossBwtEthBestBidPrice = cossBwtEthOrderBook.BestBid().Price;
            if (cossBwtEthBestBidPrice <= 0) { throw new ApplicationException("Coss's best BWT-ETH bid price should be > 0."); }

            var cossBwtEthBestAskPrice = cossBwtEthOrderBook.BestAsk().Price;
            if (cossBwtEthBestAskPrice <= 0) { throw new ApplicationException("Coss's best BWT-ETH ask price should be > 0."); }

            if (cossBwtEthBestBidPrice >= cossBwtEthBestAskPrice) { throw new ApplicationException("Coss's best BWT-ETH bid price should be less than its best ask price."); }

            var cossBwtTusdOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, "BWT", "TUSD", CachePolicy.ForceRefresh);
            var cossBwtTusdBestBidPrice = cossBwtTusdOrderBook.BestBid().Price;
            if (cossBwtTusdBestBidPrice <= 0) { throw new ApplicationException("Coss's best BWT-TUSD bid price should be > 0."); }

            var cossBwtTusdBestAskPrice = cossBwtTusdOrderBook.BestAsk().Price;
            if (cossBwtTusdBestAskPrice <= 0) { throw new ApplicationException("Coss's best BWT-TUSD ask price should be > 0."); }
            if (cossBwtTusdBestBidPrice >= cossBwtTusdBestAskPrice) { throw new ApplicationException("Coss's best BWT-TUSD bid should be less than its best ask."); }

            // var cossBwtGusdOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, "BWT", "GUSD", CachePolicy.ForceRefresh);

            binanceTask.Wait();

            // 0.00022333
            var binanceTusdBtcBestBidPrice = binanceTusdBtcOrderBook.BestBid().Price;
            if (binanceTusdBtcBestBidPrice <= 0) { throw new ApplicationException("Binances best TUSD-BTC bid price should be > 0."); }

            var binanceTusdBtcBestAskPrice = binanceTusdBtcOrderBook.BestAsk().Price;
            if (binanceTusdBtcBestAskPrice <= 0) { throw new ApplicationException("Binances best TUSD-BTC ask price should be > 0."); }

            if (binanceTusdBtcBestBidPrice >= binanceTusdBtcBestAskPrice) { throw new ApplicationException("Binances best TUSD-BTC bid price should be less than its best ask price."); }

            var binanceTusdEthBestBidPrice = binanceTusdEthOrderBook.BestBid().Price;
            if (binanceTusdEthBestBidPrice <= 0) { throw new ApplicationException("Binances best TUSD-ETH bid price should be > 0."); }

            var binanceTusdEthBestAskPrice = binanceTusdEthOrderBook.BestAsk().Price;
            if (binanceTusdEthBestAskPrice <= 0) { throw new ApplicationException("Binances best TUSD-ETH ask price should be > 0."); }

            if (binanceTusdEthBestBidPrice >= binanceTusdEthBestAskPrice) { throw new ApplicationException("Binances best TUSD-ETH bid price should be less than its best ask price."); }

            var cossBwtTusdOpenBids = cossBwtTusdOpenOrders.Where(item => item.OrderType == OrderType.Bid).ToList();
            var shouldCancelAllOpenBids = false;
            OpenOrder existingBwtTusdOpenBid = null;
            if (cossBwtTusdOpenBids.Count >= 2)
            {
                shouldCancelAllOpenBids = true;            
            }
            else if(cossBwtTusdOpenBids.Count == 1)
            {
                existingBwtTusdOpenBid = cossBwtTusdOpenBids.Single();
                if (existingBwtTusdOpenBid.Price > cossBwtTusdBestBidPrice)
                {
                    shouldCancelAllOpenBids = true;
                }
                else
                {
                    // remove our open bid from the order book so that we can recalculate the bid that we'd like to place.
                    var match = cossBwtTusdOrderBook.Bids.FirstOrDefault(item => item.Price == existingBwtTusdOpenBid.Price && item.Quantity == existingBwtTusdOpenBid.Quantity);
                    if (match != null)
                    {
                        cossBwtTusdOrderBook.Bids.Remove(match);
                    }
                    else
                    {
                        shouldCancelAllOpenBids = true;
                    }
                }
            }

            var shouldReloadCossBwtTusdOrderBook = false;
            if (shouldCancelAllOpenBids)
            {
                foreach (var openOrder in cossBwtTusdOpenBids)
                {
                    var match = cossBwtTusdOrderBook.Bids.FirstOrDefault(item => item.Price == openOrder.Price && item.Quantity == openOrder.Quantity);
                    if (match != null)
                    {
                        cossBwtTusdOrderBook.Bids.Remove(match);
                    }
                    else
                    {
                        shouldReloadCossBwtTusdOrderBook = true;
                    }

                    CancelOpenOrderWithRetries(IntegrationNameRes.Coss, openOrder);
                }
            }

            if (shouldReloadCossBwtTusdOrderBook)
            {
                cossBwtTusdOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, "BWT", "TUSD", CachePolicy.ForceRefresh);
            }

            // check for the best bid and best ask again.
            // this must be done if we cancelled any orders or if the order book was reloaded.
            cossBwtTusdBestBidPrice = cossBwtTusdOrderBook.BestBid().Price;
            if (cossBwtTusdBestBidPrice <= 0) { throw new ApplicationException("Coss's best BWT-TUSD bid price should be > 0."); }

            cossBwtTusdBestAskPrice = cossBwtTusdOrderBook.BestAsk().Price;
            if (cossBwtTusdBestAskPrice <= 0) { throw new ApplicationException("Coss's best BWT-TUSD ask price should be > 0."); }
            if (cossBwtTusdBestBidPrice >= cossBwtTusdBestAskPrice) { throw new ApplicationException("Coss's best BWT-TUSD bid should be less than its best ask."); }

            // 0.0000034 / 0.00022333 = 0.015224107822504815
            var cossBwtBtcBestBidPriceAsBwtTusd = cossBwtBtcBestBidPrice / binanceTusdBtcBestBidPrice;
            var cossBwtEthBestBidPriceAsBwtTusd = cossBwtEthBestBidPrice / binanceTusdEthBestBidPrice;

            var idealBwtTusdBidPriceFromBtc = MathUtil.Truncate(cossBwtBtcBestBidPriceAsBwtTusd * (100.0m - IdealPercentDiff) / 100.0m, PriceDecimals);
            var idealBwtTusdBidPriceFromEth = MathUtil.Truncate(cossBwtEthBestBidPriceAsBwtTusd * (100.0m - IdealPercentDiff) / 100.0m, PriceDecimals);

            var idealBwtTusdBidPrice = idealBwtTusdBidPriceFromBtc < idealBwtTusdBidPriceFromEth ? idealBwtTusdBidPriceFromBtc : idealBwtTusdBidPriceFromEth;           

            var shouldKeepExistingBid = false;
            if (existingBwtTusdOpenBid != null)
            {
                var priceDiff = existingBwtTusdOpenBid.Price - idealBwtTusdBidPrice;
                var priceRatio = priceDiff / idealBwtTusdBidPrice;
                var pricePercentDiff = 100.0m * priceRatio;
                var absPricePercentDiff = Math.Abs(pricePercentDiff);

                var quantityDiff = existingBwtTusdOpenBid.Quantity - quantity;
                var quantityRatio = quantityDiff / existingBwtTusdOpenBid.Quantity;
                var quantityPercentDiff = 100.0m * quantityRatio;
                var absQuantityPercentDiff = Math.Abs(quantityPercentDiff);

                if (absPricePercentDiff < 2.0m && absQuantityPercentDiff < 10.0m)
                {
                    shouldKeepExistingBid = true;
                }
            }

            if (!shouldKeepExistingBid && existingBwtTusdOpenBid != null)
            {
                CancelOpenOrderWithRetries(IntegrationNameRes.Coss, existingBwtTusdOpenBid);
                // todo: credit the in-memory tusd balance.
            }

            if (!shouldKeepExistingBid && idealBwtTusdBidPrice > cossBwtTusdBestBidPrice)
            {
                var price = idealBwtTusdBidPrice;

                var symbol = "BWT";
                var baseSymbol = "TUSD";

                _log.Info($"About to place a bid on {IntegrationNameRes.Coss} for {quantity} {symbol} at {price} {baseSymbol}.");
                try
                {
                    var orderResult = BuyLimitWithRetryOnNoResponse(IntegrationNameRes.Coss, symbol, baseSymbol, new QuantityAndPrice
                    {
                        Quantity = quantity,
                        Price = idealBwtTusdBidPriceFromBtc
                    });

                    if (orderResult)
                    {
                        _log.Info($"Successfully placed a bid on {IntegrationNameRes.Coss} for {quantity} {symbol} at {price} {baseSymbol}.");
                    }
                    else
                    {
                        _log.Error($"Failed to place a bid on {IntegrationNameRes.Coss} for {quantity} {symbol} at {price} {baseSymbol}.");
                    }
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to place a bid on {IntegrationNameRes.Coss} for {quantity} {symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                }
            }
        }

        public void AcquireEos()
        {
            const decimal MaxQuantityToOwn = 20.0m;

            const decimal EosTusdPriceTick = 0.00000001m;
            const int EosTusdPriceDecimals = 8;
            const string Symbol = "EOS";

            const decimal OptimumPercentDiff = 15.0m;
            const decimal MinPercentDiff = 5.0m;

            OrderBook binanceEthBtcOrderBook = null;
            OrderBook binanceEosBtcOrderBook = null;
            OrderBook binanceTusdBtcOrderBook = null;

            var binanceTask = LongRunningTask.Run(() =>
            {
                binanceEthBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, "ETH", "BTC", CachePolicy.ForceRefresh);
                binanceEosBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, Symbol, "BTC", CachePolicy.ForceRefresh);
                binanceTusdBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, "TUSD", "BTC", CachePolicy.ForceRefresh);
            });

            CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, Symbol, "TUSD");

            var balances = GetCossBalancesWithRetries(CachePolicy.ForceRefresh);
            var eosTotalBalance = balances.GetTotalForSymbol(Symbol);

            // var cossEosCossOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, Symbol, "COS", CachePolicy.ForceRefresh);
            // var cossEosEthOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, Symbol, "ETH", CachePolicy.ForceRefresh);
            var cossEosBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, Symbol, "BTC", CachePolicy.ForceRefresh);
            var cossEosTusdOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, Symbol, "TUSD", CachePolicy.ForceRefresh);

            binanceTask.Wait();

            var binanceTusdBtcBestBidPrice = binanceTusdBtcOrderBook.BestBid().Price;
            if (binanceTusdBtcBestBidPrice <= 0) { throw new ApplicationException("Binance BTC-TUSD best bid price should be > 0."); }

            var binanceTusdBtcBestAskPrice = binanceTusdBtcOrderBook.BestAsk().Price;
            if (binanceTusdBtcBestAskPrice <= 0) { throw new ApplicationException("Binance BTC-TUSD best ask price should be > 0."); }

            var binanceTusdBtcAveragePrice = new List<decimal> { binanceTusdBtcBestBidPrice, binanceTusdBtcBestAskPrice }.Average();

            var cossEosBtcBestBidPrice = cossEosBtcOrderBook.BestBid().Price;
            if (cossEosBtcBestBidPrice <= 0) { throw new ApplicationException($"Coss's {Symbol}-BTC best bid price should be > 0."); }

            var cossEosBtcBestAskPrice = cossEosBtcOrderBook.BestAsk().Price;
            if (cossEosBtcBestAskPrice <= 0) { throw new ApplicationException($"Coss's {Symbol}-BTC best ask price should be > 0."); }

            if (cossEosBtcBestBidPrice >= cossEosBtcBestAskPrice)
            {
                throw new ApplicationException($"Coss's {Symbol}-BTC bid price should not be >= its ask price.");
            }

            var cossEosTusdBestAskPrice = cossEosTusdOrderBook.BestAsk().Price;
            if (cossEosTusdBestAskPrice <= 0) { throw new ApplicationException($"Coss's {Symbol}-TUSD best ask price should be > 0."); }

            var cossEosTusdBestBidPrice = cossEosTusdOrderBook.BestBid().Price;
            if (cossEosTusdBestBidPrice <= 0) { throw new ApplicationException($"Coss's {Symbol}-TUSD best bid price should be > 0."); }

            if (cossEosTusdBestBidPrice >= cossEosTusdBestAskPrice)
            {
                throw new ApplicationException($"Coss's {Symbol}-TUSD bid price should not be >= its ask price.");
            }

            decimal? eosTusdBidPrice = null;

            var optimumBidPriceAsBtc = cossEosBtcBestBidPrice * (100.0m - OptimumPercentDiff) / 100.0m;
            var optimumBidPriceAsTusd = MathUtil.Truncate(optimumBidPriceAsBtc / binanceTusdBtcAveragePrice, EosTusdPriceDecimals);

            if (eosTotalBalance < MaxQuantityToOwn)
            {
                if (optimumBidPriceAsTusd > cossEosTusdBestBidPrice)
                {
                    eosTusdBidPrice = optimumBidPriceAsTusd;
                }
                else
                {
                    var upTickEosTusdBidPrice = cossEosTusdBestBidPrice + EosTusdPriceTick;
                    var upTickEosTusdBidPriceAsBtc = upTickEosTusdBidPrice * binanceTusdBtcAveragePrice;
                    var diff = cossEosBtcBestBidPrice - upTickEosTusdBidPriceAsBtc;
                    var ratio = diff / cossEosBtcBestBidPrice;
                    var percentDiff = 100.0m * ratio;

                    if (percentDiff >= MinPercentDiff)
                    {
                        eosTusdBidPrice = upTickEosTusdBidPrice;
                    }
                }
            }

            if (eosTusdBidPrice.HasValue)
            {
                var baseSymbol = "TUSD";

                var quantity = 5;
                var price = eosTusdBidPrice.Value;

                try
                {
                    _log.Info($"About to place a bid on Coss for {quantity} {Symbol} at {price} {baseSymbol}.");

                    var orderResult = _exchangeClient.BuyLimit(IntegrationNameRes.Coss, Symbol, baseSymbol, new QuantityAndPrice
                    {
                        Quantity = quantity,
                        Price = price
                    });

                    if (orderResult)
                    {
                        _log.Info($"Successfully placed a bid on Coss for {quantity} {Symbol} at {price} {baseSymbol}.");
                    }
                    else
                    {
                        _log.Error($"Failed to place a bid on Coss for {quantity} {Symbol} at {price} {baseSymbol}.");
                    }
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to place a bid on Coss for {quantity} {Symbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                }
            }
        }

        public void OpenBid()
        {
            var symbolsToAvoid = new List<string>
            {
                "ETH",
                "USD"
            }.Union(CossDisabledSymbols)
            .ToList();

            var baseSymbolsToAvoid = new List<string> { "USD" };

            const CachePolicy tradingPairsCachePolicy = CachePolicy.AllowCache;

            var cossTradingPairsTask = LongRunningTask.Run(() => _exchangeClient.GetTradingPairs(IntegrationNameRes.Coss, tradingPairsCachePolicy));
            var binanceTradingPairsTask = LongRunningTask.Run(() => _exchangeClient.GetTradingPairs(IntegrationNameRes.Binance, tradingPairsCachePolicy));

            var cossTradingPairs = cossTradingPairsTask.Result;
            var binanceTradingPairs = binanceTradingPairsTask.Result;

            var intersections = GetIntersections(cossTradingPairs, binanceTradingPairs);

            var knownOpenOrders = _exchangeClient.GetOpenOrders(IntegrationNameRes.Coss, CachePolicy.OnlyUseCache);
            var tradingPairsForKnownOpenOrders = knownOpenOrders.Select(item =>
                $"{item.Symbol.ToUpper()}_{item.BaseSymbol.ToUpper()}"
            ).Distinct()
            .Select(item =>
            {
                var pieces = item.Split('_');
                return new { Symbol = pieces[0], BaseSymbol = pieces[1] };
            });

            var preferred = intersections.Where(queryIntersection =>           
                tradingPairsForKnownOpenOrders.Any(tp =>
                    string.Equals(tp.Symbol, queryIntersection.Symbol, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(tp.BaseSymbol, queryIntersection.BaseSymbol, StringComparison.InvariantCultureIgnoreCase))
            ).ToList();

            // prepend the ones that we have open orders with
            // so that they get run twice as often.
            var tradingPairsToUse = preferred.Union(intersections).ToList();

            for (var index = 0; index < tradingPairsToUse.Count; index++)
            {
                var intersection = tradingPairsToUse[index];

                if (symbolsToAvoid.Any(querySymbol => string.Equals(intersection.Symbol, querySymbol, StringComparison.InvariantCultureIgnoreCase)))
                { continue; }

                if (baseSymbolsToAvoid.Any(queryBaseSymbol => string.Equals(intersection.BaseSymbol, queryBaseSymbol, StringComparison.InvariantCultureIgnoreCase)))
                { continue; }

                try
                {
                    OpenBid(intersection.Symbol, intersection.BaseSymbol);
                }
                catch(Exception exception)
                {
                    _log.Error(exception);
                    Thread.Sleep(TimeSpan.FromSeconds(15));
                    try
                    {
                        OpenBid(intersection.Symbol, intersection.BaseSymbol);
                    }
                    catch(Exception exceptionB)
                    {                        
                        _log.Error(exceptionB);
                        Thread.Sleep(TimeSpan.FromMinutes(1));
                        continue;
                    }
                }
            }
        }

        public void OpenBid(string symbol, string baseSymbol)
        {
            _log.Debug($"Beginning open bid process for {symbol}-{baseSymbol}.");

            const decimal IdealPercentage = 10;
            const decimal WorstAcceptablePercentage = 5;

            OrderBook binanceOrderBook;

            try
            {
                binanceOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, symbol, baseSymbol, CachePolicy.ForceRefresh);
            }
            catch (Exception exception)
            {
                _log.Error(exception);

                Thread.Sleep(TimeSpan.FromSeconds(15));
                binanceOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, symbol, baseSymbol, CachePolicy.ForceRefresh);
            }
           
            var openOrders = _exchangeClient.GetOpenOrders(IntegrationNameRes.Coss, symbol, baseSymbol, CachePolicy.ForceRefresh);
            var myOpenBids = (openOrders ?? new List<OpenOrderForTradingPair>())
                .Where(queryOpenOrder => queryOpenOrder.OrderType == OrderType.Bid)
                .ToList();

            var cossOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Coss, symbol, baseSymbol, CachePolicy.ForceRefresh);
            var bestCossBid = cossOrderBook.BestBid();

            var bestBinanceBid = binanceOrderBook.BestBid();
            if (bestBinanceBid == null || bestCossBid == null)
            {
                // Something went wrong.
                // To be safe, cancel any open bids before moving on.
                foreach (var myOpenBid in myOpenBids)
                {
                    _exchangeClient.CancelOrder(IntegrationNameRes.Coss, myOpenBid.OrderId);
                }

                return;
            }

            var bestCossBidPrice = bestCossBid.Price;
            var bestBinanceBidPrice = bestBinanceBid.Price;

            bool wereAnyOrdersCancelled = false;
            if (myOpenBids != null && myOpenBids.Any())
            {
                // TODO: For now, if there are multiple open
                // TODO: orders, just cancel them all.
                // TODO: In the future, it would be better
                // TODO: to only cancel all but one of them.
                if (myOpenBids.Count >= 2)
                {
                    _log.Debug($"There are {myOpenBids.Count} open bids for {symbol}-{baseSymbol}. Cancelling them.");
                    foreach (var myOpenBid in myOpenBids)
                    {
                        _exchangeClient.CancelOrder(IntegrationNameRes.Coss, myOpenBid.OrderId);
                    }

                    wereAnyOrdersCancelled = true;
                }
                else
                {
                    var myOpenBid = myOpenBids.Single();
                    var openBidDiff = bestBinanceBidPrice - myOpenBid.Price;
                    var openBidDiffRatio = openBidDiff / bestCossBidPrice;
                    var openBidPercentage = 100.0m * openBidDiffRatio;

                    if (myOpenBid.Price < bestCossBidPrice)
                    {
                        _log.Debug($"Our bid for {symbol} at {myOpenBid.Price} {baseSymbol} is losing to someone else's bid at {bestCossBidPrice} {baseSymbol}." + 
                            $"{Environment.NewLine}Cancelling our bid.");
                        _exchangeClient.CancelOrder(IntegrationNameRes.Coss, myOpenBid.OrderId);
                        wereAnyOrdersCancelled = true;                        
                    }
                    else if (openBidPercentage < WorstAcceptablePercentage)
                    {
                        _log.Debug($"Our bid for {symbol} at {myOpenBid.Price} {baseSymbol} would only give us a {openBidPercentage}% gain and that's below the worst acceptable percentage of {WorstAcceptablePercentage} %." +
                            $"{Environment.NewLine}Cancelling our bid.");
                        _exchangeClient.CancelOrder(IntegrationNameRes.Coss, myOpenBid.OrderId);
                        wereAnyOrdersCancelled = true;
                    }
                    else
                    {
                        _log.Debug($"Bid for {symbol} at {myOpenBid.Price} {baseSymbol} is still good with {openBidPercentage}% potential gain.");
                        return;
                    }
                }
            }

            if (wereAnyOrdersCancelled)
            {
                cossOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Coss, symbol, baseSymbol, CachePolicy.ForceRefresh);

                bestCossBid = cossOrderBook.BestBid();
                if (bestCossBid == null) { return; }
                bestCossBidPrice = bestCossBid.Price;
            }           

            var diff = bestBinanceBidPrice - bestCossBidPrice;
            var diffRatio = diff / bestBinanceBidPrice;
            var percentDiff = 100.0m * diffRatio;

            var autoOpenBid = new AutoOpenBid();
            var bidPrice = autoOpenBid.ExecuteAgainstHighVolumeExchange(cossOrderBook, binanceOrderBook, 1.0m - (IdealPercentage / 100.0m), 1.0m - (WorstAcceptablePercentage / 100.0m));

            if (bidPrice == null)
            {
                // _log.Debug($"Nothing for {symbol}-{baseSymbol}.");
                return;
            }

            const decimal DesiredEthTrade = 0.15m;
            const decimal DesiredBtcTrade = 0.01m;
            var desiredBaseSymbolTradeDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "ETH", DesiredEthTrade },
                { "BTC", DesiredBtcTrade }
            };

            if (!desiredBaseSymbolTradeDictionary.ContainsKey(baseSymbol))
            {
                _log.Debug($"Couldn't determine the desired trade quantity for base symbol {baseSymbol}");
                return;
            }

            var desiredBaseSymbolTrade = desiredBaseSymbolTradeDictionary[baseSymbol];
            var tradeQuantity = desiredBaseSymbolTrade / bidPrice.Value;

            _log.Debug($"Placing a bid for {tradeQuantity.ToString("N6")} {symbol} at {bidPrice.Value.ToString("N6")} {baseSymbol} with a {percentDiff} % potential gain.");
            _log.Info($"Placing a bid for {tradeQuantity.ToString("N6")} {symbol} at {bidPrice.Value.ToString("N6")} {baseSymbol} with a {percentDiff} % potential gain.");

            try
            {
                _exchangeClient.BuyLimit(IntegrationNameRes.Coss, symbol, baseSymbol, new QuantityAndPrice { Quantity = tradeQuantity, Price = bidPrice.Value });
            }
            finally
            {
                _exchangeClient.GetOpenOrders(IntegrationNameRes.Coss, symbol, baseSymbol, CachePolicy.ForceRefresh);
            }
        }

        private static List<TradingPair> GetIntersections(List<TradingPair> tradingPairsA, List<TradingPair> tradingPairsB)
        {
            return tradingPairsA.Where(queryTradingPairA =>
                tradingPairsB.Any(queryTradingPairB =>
                    DoTradingPairsMatch(queryTradingPairB, queryTradingPairA))
            ).ToList();
        }

        private static bool DoTradingPairsMatch(TradingPair pairA, TradingPair pairB)
        {
            if (pairA == null && pairB == null) { return true; }
            if (pairA == null || pairB == null) { return false; }

            if (pairA.CanonicalCommodityId.HasValue
                && pairA.CanonicalCommodityId.Value != default(Guid)
                && pairB.CanonicalCommodityId.HasValue
                && pairB.CanonicalCommodityId.Value != default(Guid))
            {
                if (pairA.CanonicalCommodityId.Value != pairB.CanonicalCommodityId.Value) { return false; }
            }
            else
            {
                if (!string.Equals(pairA.Symbol, pairB.Symbol, StringComparison.InvariantCultureIgnoreCase))
                {
                    return false;
                }
            }

            if (pairA.CanonicalBaseCommodityId.HasValue
                && pairA.CanonicalBaseCommodityId.Value != default(Guid)
                && pairB.CanonicalBaseCommodityId.HasValue
                && pairB.CanonicalBaseCommodityId.Value != default(Guid))
            {
                if (pairA.CanonicalBaseCommodityId.Value != pairB.CanonicalBaseCommodityId.Value) { return false; }
            }
            else
            {
                if (!string.Equals(pairA.BaseSymbol, pairB.BaseSymbol, StringComparison.InvariantCultureIgnoreCase))
                {
                    return false;
                }
            }

            return true;
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
            { "COS", MinimumTradeCoss },
            { "TUSD",MinimumTradeTusd },
            { "USDT", MinimumTradeUsdt },
            { "XRP", MinimumTradeXrp }
        };

        private HoldingInfo GetCossBalancesWithRetries(CachePolicy cachePolicy)
        {
            return AttemptWithRetries(() =>
            {
                var result = _exchangeClient.GetBalances(IntegrationNameRes.Coss, cachePolicy);
                return result;
            });
        }

        private Holding GetCossBalanceWithRetries(string symbol, CachePolicy cachePolicy)
        {
            return AttemptWithRetries(() => _exchangeClient.GetBalance(IntegrationNameRes.Coss, symbol, cachePolicy));
        }

        private OrderBook GetOrderBookWithRetries(string exchange, string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            _log.Info($"Getting {exchange} order book for {symbol}-{baseSymbol} with cache policy {cachePolicy}.");
            return AttemptWithRetries(() =>
            {
                var orderBook = _exchangeClient.GetOrderBook(exchange, symbol, baseSymbol, cachePolicy);
                if(orderBook == null) { throw new ApplicationException($"Received a null order book from {exchange} for {symbol}-{baseSymbol} with cache policy {cachePolicy}."); }

                return orderBook;
            });
        }

        private bool BuyLimitWithRetryOnNoResponse(string exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice)
        {
            try
            {
                return _exchangeClient.BuyLimit(exchange, symbol, baseSymbol, quantityAndPrice);
            }
            catch (Exception exception)
            {
                if (exception.Message.ToUpper().Contains("No Response".ToUpper()))
                {
                    const double DelaySeconds = 2.5d;
                    _log.Error($"No response when attempting to place buy limit on {exchange} for {quantityAndPrice.Quantity} {symbol} at {quantityAndPrice.Price} {baseSymbol}. Trying again in {DelaySeconds} seconds.");
                    Thread.Sleep(TimeSpan.FromSeconds(DelaySeconds));

                    return _exchangeClient.BuyLimit(exchange, symbol, baseSymbol, quantityAndPrice);
                }
                else
                {
                    throw;
                }
            }
        }

        private bool SellLimitWithRetryOnNoResponse(string exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice)
        {
            try
            {
                return _exchangeClient.SellLimit(exchange, symbol, baseSymbol, quantityAndPrice);
            }
            catch (Exception exception)
            {
                if (exception.Message.ToUpper().Contains("No Response".ToUpper()))
                {
                    const double DelaySeconds = 2.5d;
                    _log.Error($"No response when attempting to place sell limit on {exchange} for {quantityAndPrice.Quantity} {symbol} at {quantityAndPrice.Price} {baseSymbol}. Trying again in {DelaySeconds} seconds.");
                    Thread.Sleep(TimeSpan.FromSeconds(DelaySeconds));

                    return _exchangeClient.SellLimit(exchange, symbol, baseSymbol, quantityAndPrice);
                }
                else
                {
                    throw;
                }
            }
        }

        private HoldingInfo GetBalancesWithRetries(string exchange, CachePolicy cachePolicy)
        {
            return AttemptWithRetries(() => _exchangeClient.GetBalances(exchange, cachePolicy));
        }

        private List<OpenOrderForTradingPair> GetOpenOrdersWithRetries(string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            var exchange = IntegrationNameRes.Coss;

            _log.Info($"Getting {exchange} open orders for {symbol}-{baseSymbol} with cache policy {cachePolicy}.");
            var retriever = new Func<List<OpenOrderForTradingPair>>(() => _exchangeClient.GetOpenOrders(exchange, symbol, baseSymbol, cachePolicy));
            return AttemptWithRetries(retriever);
        }

        private OpenOrdersWithAsOf GetOpenOrdersForTradingPairV2WithRetries(string exchange, string symbol, string baseSymbol, CachePolicy cachePolicy)
        {
            _log.Info($"Getting {exchange} v2 open orders for {symbol}-{baseSymbol} with cache policy {cachePolicy}.");
            return AttemptWithRetries(() => _exchangeClient.GetOpenOrdersForTradingPairV2(exchange, symbol, baseSymbol, cachePolicy));
        }

        private void CancelOpenOrdersWithRetries(string exchange, List<OpenOrderForTradingPair> openOrders)
        {
            foreach (var openOrder in openOrders ?? new List<OpenOrderForTradingPair>())
            {
                CancelOpenOrderWithRetries(exchange, openOrder);
            }
        }

        private void CancelOpenOrdersWithRetries(string exchange, List<OpenOrder> openOrders)
        {
            foreach (var openOrder in openOrders ?? new List<OpenOrder>())
            {
                CancelOpenOrderWithRetries(exchange, openOrder);
            }
        }

        private void CancelOpenOrderWithRetries(string exchange, OpenOrder openOrder)
        {
            CancelOpenOrderWithRetries(exchange, openOrder.OrderId);
        }

        private void CancelOpenOrderWithRetries(string exchange, string orderId)
        {
            _log.Info($"Cancelling {exchange} order {orderId}.");
            AttemptWithRetries(() => _exchangeClient.CancelOrder(exchange, orderId));
        }

        private List<TradingPair> GetTradingPairsWithRetries(string exchange, CachePolicy cachePolicy)
        {
            _log.Info($"Getting {exchange} trading pairs with cache policy {cachePolicy}.");
            return AttemptWithRetries(() => _exchangeClient.GetTradingPairs(exchange, cachePolicy));
        }

        private void AttemptWithRetries(Action method)
        {
            AttemptWithRetries(new Func<int>(() =>
            {
                method();
                return 1;
            }));
        }

        private T AttemptWithRetries<T>(Func<T> method, int maxAttempts = 3, double sleepGrowthRate = 2.5d)
        {
            Exception lastException = null;

            for (var i = 0; i < maxAttempts; i++)
            {
                try
                {
                    if (i != 0)
                    {
                        var secondsToSleep = i * sleepGrowthRate;
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

        public void AcquireEthLtc()
        {
            const string Symbol = "ETH";
            const string BaseSymbol = "LTC";

            OrderBook binanceLtcEthOrderBook = null;

            var binanceTask = LongRunningTask.Run(() =>
            {
                var binanceEthLtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, "LTC", "ETH", CachePolicy.ForceRefresh);
                binanceLtcEthOrderBook = InvertOrderBook(binanceEthLtcOrderBook);
            });

            var cossTradingPairs = GetTradingPairsWithRetries(IntegrationNameRes.Coss, CachePolicy.AllowCache);
            var cossEthLtcTradingPair = cossTradingPairs.SingleOrDefault(item => string.Equals(item.Symbol, Symbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(item.BaseSymbol, BaseSymbol, StringComparison.InvariantCultureIgnoreCase));

            if (!cossEthLtcTradingPair.PriceTick.HasValue)
            {
                throw new ApplicationException($"Coss {Symbol}-{BaseSymbol} does not have a price tick configured.");
            }

            var priceTick = cossEthLtcTradingPair.PriceTick.Value;

            CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, Symbol, BaseSymbol);

            var cossEthLtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, Symbol, BaseSymbol, CachePolicy.ForceRefresh);

            var cossEthLtcBestBidPrice = cossEthLtcOrderBook.BestBid().Price;
            if (cossEthLtcBestBidPrice <= 0) { throw new ApplicationException($"Coss's {Symbol}-{BaseSymbol} best bid price should be > 0. "); }

            var cossEthLtcBestAskPrice = cossEthLtcOrderBook.BestAsk().Price;
            if (cossEthLtcBestAskPrice <= 0) { throw new ApplicationException($"Coss's {Symbol}-{BaseSymbol} best ask price should be > 0. "); }

            if (cossEthLtcBestBidPrice >= cossEthLtcBestAskPrice) { throw new ApplicationException($"Coss's {Symbol}-{BaseSymbol} best bid price should be lower than its best ask price."); }

            binanceTask.Wait();

            var binanceLtcEthBestBidPrice = binanceLtcEthOrderBook.BestBid().Price;
            if (binanceLtcEthBestBidPrice <= 0) { throw new ApplicationException($"Binance's {Symbol}-{BaseSymbol} best bid price should be > 0. "); }

            var binanceLtcEthBestAskPrice = binanceLtcEthOrderBook.BestAsk().Price;
            if (binanceLtcEthBestAskPrice <= 0) { throw new ApplicationException($"Binance's {Symbol}-{BaseSymbol} best ask price should be > 0. "); }

            if (binanceLtcEthBestBidPrice >= binanceLtcEthBestAskPrice) { throw new ApplicationException("Binance's ETH-LTC best bid price should be lower than its best ask price."); }

            decimal? priceToBid = null;
            var optimumBidPrice = binanceLtcEthBestBidPrice * 0.85m;
            if (optimumBidPrice > cossEthLtcBestBidPrice)
            {
                priceToBid = optimumBidPrice;
            }
            else
            {
                var upTickBidPrice = cossEthLtcBestBidPrice + priceTick;
                var diff = binanceLtcEthBestBidPrice - upTickBidPrice;
                var ratio = diff / binanceLtcEthBestBidPrice;
                var percentDiff = 100.0m * ratio;
                if (percentDiff >= MinOpenBidPercentDiff)
                {
                    priceToBid = upTickBidPrice;
                }
            }

            var quantityToBid = 0.1m;

            if (priceToBid.HasValue)
            {
                var price = priceToBid.Value;
                var quantity = quantityToBid;
                _log.Info($"About to place a bid on Coss for {quantity} {Symbol} at {price} {BaseSymbol}.");
                try
                {
                    var orderResult = _exchangeClient.BuyLimitV2(IntegrationNameRes.Coss, Symbol, BaseSymbol, new QuantityAndPrice
                    {
                        Quantity = quantity,
                        Price = price
                    });

                    if (orderResult.WasSuccessful)
                    {
                        _log.Info($"Successfully placed a bid on Coss for {quantity} {Symbol} at {price} {BaseSymbol}.");
                    }
                    else
                    {
                        _log.Info($"Failed to place a bid on Coss for {quantity} {Symbol} at {price} {BaseSymbol}.");
                    }
                }
                catch (Exception exception)
                {
                    _log.Info($"Failed to place a bid on Coss for {quantity} {Symbol} at {price} {BaseSymbol}.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                }
            }
        }

        public void AcquireLtc()
        {
            const string AcquisitionSymbol = "LTC";
            const decimal TradeFeeRatio = 0.0016m; // This is normally 0.12%, but can go up to 0.15%. Taking it up to 0.16% to prevent rounding errors.
            const decimal OptimumProfitPercentDiff = 15.0m;
            const decimal MinBidPercentDiff = 2.0m;
            const decimal MinAskPercentDiff = 2.0m;
            const decimal PriceTick = 0.00000001m;

            const decimal MinEthAskQuantity = 0.01m;
            decimal maxEthAskQuantity = 0.5m;

            const decimal MinLtcTradeQuantity = 0.01m;
            const decimal MaxLtcTradeQuantity = 1.0m;

            List<TradingPair> cossTradingPairs = null;

            OrderBook binanceLtcEthOrderBook = null;
            OrderBook binanceEthLtcOrderBook = null;
            OrderBook binanceLtcBtcOrderBook = null;

            OrderBook cossEthLtcOrderBook = null;

            HoldingInfo cossHoldings = null;

            var binanceGetOrderBooksTask = LongRunningTask.Run(() =>
            {
                binanceLtcEthOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, AcquisitionSymbol, "ETH", CachePolicy.ForceRefresh);
                binanceEthLtcOrderBook = InvertOrderBook(binanceLtcEthOrderBook);

                binanceLtcBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, AcquisitionSymbol, "BTC", CachePolicy.ForceRefresh);
            });

            CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, "ETH", AcquisitionSymbol);

            var cossInitialLoadTask = LongRunningTask.Run(() =>
            {
                cossTradingPairs = GetTradingPairsWithRetries(IntegrationNameRes.Coss, CachePolicy.AllowCache);
                cossEthLtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, "ETH", AcquisitionSymbol, CachePolicy.ForceRefresh);
                cossHoldings = GetBalancesWithRetries(IntegrationNameRes.Coss, CachePolicy.ForceRefresh);
            });

            binanceGetOrderBooksTask.Wait();
            cossInitialLoadTask.Wait();

            var ethAvailable = cossHoldings.GetAvailableForSymbol("ETH");
            var ltcAvailable = cossHoldings.GetAvailableForSymbol(AcquisitionSymbol);
            var btcAvailable = cossHoldings.GetAvailableForSymbol("BTC");
            var cossAvailable = cossHoldings.GetAvailableForSymbol("COS");
            var tusdAvailable = cossHoldings.GetAvailableForSymbol("TUSD");

            maxEthAskQuantity = 0.5m;
            if (ethAvailable < 0.5m)
            {
                maxEthAskQuantity = 0.1m;
            }
            else if (ethAvailable < 0.75m)
            {
                maxEthAskQuantity = 0.2m;
            }
            else if (ethAvailable < 1.0m)
            {
                maxEthAskQuantity = 0.35m;
            }

            // ETH-LTC           
            var binanceBestEthLtcBid = binanceEthLtcOrderBook.BestBid();
            var binanceBestEthLtcBidPrice = binanceBestEthLtcBid.Price;
            if (binanceBestEthLtcBidPrice <= 0) { throw new ApplicationException("Binance best eth ltc bid price should be > 0."); }

            var binanceBestEthLtcAsk = binanceEthLtcOrderBook.BestAsk();
            var binanceBestEthLtcAskPrice = binanceBestEthLtcAsk.Price;
            if (binanceBestEthLtcAskPrice <= 0) { throw new ApplicationException("Binance best eth ltc ask price should be > 0."); }

            var cossBestEthLtcBid = cossEthLtcOrderBook.BestBid();
            var cossBestEthLtcBidPrice = cossBestEthLtcBid.Price;
            if (cossBestEthLtcBidPrice <= 0) { throw new ApplicationException("Coss best eth ltc bid price should be > 0."); }

            var cossBestEthLtcAsk = cossEthLtcOrderBook.BestAsk();
            var cossBestEthLtcAskPrice = cossBestEthLtcAsk.Price;
            if (cossBestEthLtcAskPrice <= 0) { throw new ApplicationException("Coss best eth ltc ask price should be > 0."); }

            decimal? ethLtcBidPriceToPlace = null;
            var optimalEthLtcBidPrice = binanceBestEthLtcBidPrice - (1.0m + OptimumProfitPercentDiff / 100.0m);
            if (optimalEthLtcBidPrice > cossBestEthLtcBidPrice)
            {
                ethLtcBidPriceToPlace = optimalEthLtcBidPrice;
            }
            else
            {
                var minimalStepUpBid = cossBestEthLtcBidPrice + PriceTick;
                var diff = binanceBestEthLtcBidPrice - minimalStepUpBid;
                var diffRatio = diff / binanceBestEthLtcBidPrice;
                var diffPercent = 100.0m * diffRatio;

                if (diffPercent >= MinBidPercentDiff)
                {
                    ethLtcBidPriceToPlace = minimalStepUpBid;
                }
            }

            if (ethLtcBidPriceToPlace.HasValue)
            {
                var symbol = "ETH";
                var baseSymbol = AcquisitionSymbol;

                var price = ethLtcBidPriceToPlace.Value;
                var maxPossibleQuantity = ltcAvailable / ((1.0m + 0.15m / 100.0m) * price);
                var quantity = maxPossibleQuantity;
                if (quantity > MaxLtcTradeQuantity) { quantity = MaxLtcTradeQuantity; }

                var lotSize = GetLotSize(cossTradingPairs, symbol, baseSymbol);
                quantity = MathUtil.ConstrainToMultipleOf(quantity, lotSize);

                if (quantity >= MinLtcTradeQuantity)
                {
                    _log.Info($"About to place a bid for {quantity} {symbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}");
                    try
                    {
                        var orderResult = _exchangeClient.BuyLimit(IntegrationNameRes.Coss, symbol, baseSymbol, new QuantityAndPrice
                        {
                            Price = price,
                            Quantity = quantity
                        });

                        if (orderResult)
                        {
                            ltcAvailable -= price * quantity * (1.0m + TradeFeeRatio);
                            _log.Info($"Successfully placed a bid for {quantity} {symbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}");
                        }
                        else
                        {
                            _log.Error($"Failed to place a bid for {quantity} {symbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place a bid for {quantity} {symbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.{Environment.NewLine}{exception.Message}");
                    }
                }
            }

            decimal? ethLtcAskPriceToPlace = null;
            var optimalAsk = binanceBestEthLtcAskPrice * (1.0m + OptimumProfitPercentDiff / 100.0m);
            if (optimalAsk < cossBestEthLtcAskPrice)
            {
                ethLtcBidPriceToPlace = optimalAsk;
            }
            else
            {
                var minimumStepDownAsk = cossBestEthLtcAskPrice - PriceTick;
                var diff = minimumStepDownAsk - binanceBestEthLtcAskPrice;
                var diffRatio = diff / binanceBestEthLtcAskPrice;
                var diffPercent = 100.0m * diffRatio;

                if (diffPercent >= MinAskPercentDiff)
                {
                    ethLtcAskPriceToPlace = minimumStepDownAsk;
                }
            }

            if (ethLtcAskPriceToPlace.HasValue)
            {
                var symbol = "ETH";
                var baseSymbol = AcquisitionSymbol;

                var lotSize = GetLotSize(cossTradingPairs, symbol, baseSymbol);

                var price = ethLtcAskPriceToPlace.Value;
                var quantity = ethAvailable;
                if (quantity > maxEthAskQuantity) { quantity = maxEthAskQuantity; }
                quantity = MathUtil.ConstrainToMultipleOf(quantity, lotSize);

                if (quantity >= MinEthAskQuantity)
                {
                    _log.Info($"About to place an ask for {quantity} {symbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}");
                    try
                    {
                        var orderResult = SellLimitWithCompensation(IntegrationNameRes.Coss, symbol, baseSymbol, new QuantityAndPrice
                        {
                            Price = price,
                            Quantity = quantity
                        }, lotSize);

                        if (orderResult)
                        {
                            ethAvailable -= quantity * price * (1.0m + TradeFeeRatio);
                            _log.Info($"Successfully placed an ask for {quantity} {symbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}");
                        }
                        else
                        {
                            _log.Error($"Failed to place an ask for {quantity} {symbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place an ask for {quantity} {symbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.{Environment.NewLine}{exception.Message}");
                    }
                }
            }

            // LTC-BTC
            CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, AcquisitionSymbol, "BTC");

            // LTC-BTC Bid
            OrderBook cossLtcBtcOrderBook = null;
            var cossGetLtcBtcOrderBookTask = LongRunningTask.Run(() =>
            {
                cossLtcBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, AcquisitionSymbol, "BTC", CachePolicy.ForceRefresh);
            });

            cossGetLtcBtcOrderBookTask.Wait();

            var bestCossLtcBtcBid = cossLtcBtcOrderBook.BestBid();
            var bestCossLtcBtcBidPrice = bestCossLtcBtcBid.Price;
            if (bestCossLtcBtcBidPrice <= 0) { throw new ApplicationException("The best bid price on Coss LTC-BTC must be > 0."); }

            var binanceLtcBtcBestBid = binanceLtcBtcOrderBook.BestBid();
            var binanceLtcBtcBestBidPrice = binanceLtcBtcBestBid.Price;
            if (binanceLtcBtcBestBidPrice <= 0) { throw new ApplicationException("The best bid price on Binance LTC-BTC must be > 0."); }

            var cossLtcBtcBestAsk = cossLtcBtcOrderBook.BestAsk();
            var cossLtcBtcBestAskPrice = cossLtcBtcBestAsk.Price;
            if (cossLtcBtcBestAskPrice <= 0) { throw new ApplicationException("The best ask price on Coss LTC-BTC must be > 0."); }

            var binanceLtcBtcBestAsk = binanceLtcBtcOrderBook.BestAsk();
            var binanceLtcBtcBestAskPrice = binanceLtcBtcBestAsk.Price;
            if (binanceLtcBtcBestAskPrice <= 0) { throw new ApplicationException("The best ask price on Binance LTC-BTC must be > 0."); }

            decimal? ltcBtcBidPriceToPlace = null;
            var optimalLtcBtcBidPrice = binanceLtcBtcBestBidPrice * (1.0m - OptimumProfitPercentDiff / 100.0m);
            if (optimalLtcBtcBidPrice > bestCossLtcBtcBidPrice)
            {
                ltcBtcBidPriceToPlace = optimalLtcBtcBidPrice;
            }
            else
            {
                var smallIncCossLtcBtcBidPrice = bestCossLtcBtcBidPrice + PriceTick;
                var diff = binanceLtcBtcBestBidPrice - smallIncCossLtcBtcBidPrice;
                var diffRatio = diff / binanceLtcBtcBestBidPrice;
                var percentDiff = 100.0m * diffRatio;
                if (percentDiff >= MinBidPercentDiff)
                {
                    ltcBtcBidPriceToPlace = smallIncCossLtcBtcBidPrice;
                }
            }

            if (ltcBtcBidPriceToPlace.HasValue)
            {
                var quantity = 0.025m;
                var price = ltcBtcBidPriceToPlace.Value;
                try
                {
                    _log.Info($"About to place a limit bid for {quantity} LTC at {price} BTC on Coss.");
                    var orderResult = _exchangeClient.BuyLimit(IntegrationNameRes.Coss, AcquisitionSymbol, "BTC", new QuantityAndPrice
                    {
                        Price = price,
                        Quantity = quantity
                    });

                    if (orderResult)
                    {
                        btcAvailable -= quantity * price * (1.0m + TradeFeeRatio);
                        _log.Info($"Successfully placed a limit bid for {quantity} LTC at {price} BTC on Coss.");
                    }
                    else
                    {
                        _log.Error($"Failed to place a limit bid for {quantity} LTC at {price} BTC on Coss.");
                    }
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to place a limit bid for {quantity} LTC at {price} BTC on Coss.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                }
            }

            // LTC-BTC Ask
            if (ltcAvailable >= 2.5m * MinLtcTradeQuantity)
            {
                decimal? ltcBtcAskPriceToPlace = null;
                var optimalLtcBtcAskPrice = binanceLtcBtcBestAskPrice * (1.0m + OptimumProfitPercentDiff / 100.0m);
                if (optimalLtcBtcAskPrice < cossLtcBtcBestAskPrice)
                {
                    ltcBtcAskPriceToPlace = optimalLtcBtcAskPrice;
                }
                else
                {
                    var downTickPotentialAskPrice = cossLtcBtcBestAskPrice - PriceTick;
                    var diff = downTickPotentialAskPrice - binanceLtcBtcBestAskPrice;
                    var ratio = diff / binanceLtcBtcBestAskPrice;
                    var percentDiff = 100.0m * ratio;

                    if (percentDiff >= MinAskPercentDiff)
                    {
                        ltcBtcAskPriceToPlace = downTickPotentialAskPrice;
                    }
                }

                if (ltcBtcAskPriceToPlace.HasValue)
                {
                    var baseSymbol = "BTC";
                    var price = ltcBtcAskPriceToPlace.Value;
                    var quantity = ltcAvailable >= 5
                        ? 2.5m
                        : ltcAvailable / 2.0m;

                    try
                    {
                        _log.Info($"About to place a sell limit order on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        var orderResult = _exchangeClient.SellLimit(IntegrationNameRes.Coss, AcquisitionSymbol, baseSymbol, new QuantityAndPrice
                        {
                            Price = price,
                            Quantity = quantity
                        });

                        if (orderResult)
                        {
                            ltcAvailable -= price * quantity * (1.0m + TradeFeeRatio);
                            _log.Info($"Successfully placed a sell limit order on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        }
                        else
                        {
                            _log.Error($"Failed to place a sell limit order on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to place a sell limit order on Coss for {quantity} {AcquisitionSymbol} at {price} {baseSymbol}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
            }

            // LTC-TUSD
            CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, "LTC", "TUSD");
            var cossLtcTusdOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, "LTC", "TUSD", CachePolicy.ForceRefresh);
            var cossLtcTusdBestAskPrice = cossLtcTusdOrderBook.BestAsk().Price;
            var cossLtcTusdBestBidPrice = cossLtcTusdOrderBook.BestBid().Price;
            if (cossLtcTusdBestAskPrice > 0 && cossLtcTusdBestBidPrice> 0 && cossLtcTusdBestAskPrice > cossLtcTusdBestBidPrice)
            {
                var binanceTusdBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, "TUSD", "BTC", CachePolicy.ForceRefresh);
                var binanceTusdBtcBestBidPrice = binanceTusdBtcOrderBook.BestBid().Price;
                var binacneTusdBtcBestAskPrice = binanceTusdBtcOrderBook.BestAsk().Price;
                if (binanceTusdBtcBestBidPrice > 0 && binacneTusdBtcBestAskPrice > 0 && binacneTusdBtcBestAskPrice > binanceTusdBtcBestBidPrice)
                {
                    var averageBinanceTusdBtcPrice = new List<decimal> { binanceTusdBtcBestBidPrice, binacneTusdBtcBestAskPrice }.Average();
                    var cossLtcTusdBestAskPriceAsBtc = cossLtcTusdBestAskPrice * averageBinanceTusdBtcPrice;
                    var optimumLtcBtcAskPrice = binanceLtcBtcBestAskPrice * (100.0m + OptimumProfitPercentDiff) / 100.0m;
                    var optimumLtcTusdAskPrice = optimumLtcBtcAskPrice / averageBinanceTusdBtcPrice;

                    decimal? ltcTusdPriceToAsk = null;
                    if (optimumLtcTusdAskPrice < cossLtcTusdBestAskPrice)
                    {
                        ltcTusdPriceToAsk = optimumLtcTusdAskPrice;
                    }
                }

                // cossLtcTusdBestAskPriceAsBtc = cossLtcTusdBestAskPrice * 
            }


            // LTC-COSS
            //CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, AcquisitionSymbol, "COS");
            //var cossCossBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, "COS", "BTC", CachePolicy.ForceRefresh);
            //var cossLtcCossOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, AcquisitionSymbol, "COS", CachePolicy.ForceRefresh);

            //var cossCossBtcBestAskPrice = cossCossBtcOrderBook.BestAsk().Price;
            //if (cossCossBtcBestAskPrice <= 0) { throw new ApplicationException("Coss's COSS-BTC best ask price should be > 0."); }

            //var cossCossBtcBestBidPrice = cossCossBtcOrderBook.BestBid().Price;
            //if (cossCossBtcBestBidPrice <= 0) { throw new ApplicationException("Coss's COSS-BTC best bid price should be > 0."); }

            //var cossLtcCossBestAskPrice = cossLtcCossOrderBook.BestAsk().Price;
            //if (cossLtcCossBestAskPrice <= 0) { throw new ApplicationException("Coss's LTC-COSS best ask price should be > 0."); }
            //var cossLtcCossBestAskPriceAsBtc = cossLtcCossBestAskPrice * cossCossBtcBestBidPrice;
        }

        public void AcquireBchabc()
        {
            const decimal DefaultLotSize = 0.00000001m;
            const decimal DefaultPriceTick = 0.00000001m;

            const string AcquisitionSymbol = "BCHABC";
            const decimal MinAskPercentDiff = 2.0m;
            const decimal MinBidPercentDiff = 2.0m;

            const decimal OptimumBidPercentDiff = 15.0m;

            OrderBook binanceBchabcBtcOrderBook = null;
            var binanceTask = LongRunningTask.Run(() =>
            {
                binanceBchabcBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, AcquisitionSymbol, "BTC", CachePolicy.ForceRefresh);
            });

            var cossTradingPairs = _exchangeClient.GetTradingPairs(IntegrationNameRes.Coss, CachePolicy.AllowCache);
            var cossBchabcBtcTradingPair = cossTradingPairs.SingleOrDefault(item => string.Equals(AcquisitionSymbol, item.Symbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals("BTC", item.BaseSymbol, StringComparison.InvariantCultureIgnoreCase));

            var lotSize = cossBchabcBtcTradingPair != null && cossBchabcBtcTradingPair.LotSize.HasValue ? cossBchabcBtcTradingPair?.LotSize.Value : DefaultLotSize;
            var priceTick = cossBchabcBtcTradingPair != null && cossBchabcBtcTradingPair.PriceTick.HasValue ? cossBchabcBtcTradingPair.PriceTick.Value : DefaultPriceTick;

            var cossBchabcBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, AcquisitionSymbol, "BTC", CachePolicy.ForceRefresh);

            binanceTask.Wait();

            var cossBchabcBtcBestBidPrice = cossBchabcBtcOrderBook.BestBid().Price;
            if (cossBchabcBtcBestBidPrice <= 0) { throw new ApplicationException($"Coss's {AcquisitionSymbol}-BTC best bid price must be > 0."); }

            var cossBchabcBtcBestAskPrice = cossBchabcBtcOrderBook.BestAsk().Price;
            if (cossBchabcBtcBestAskPrice <= 0) { throw new ApplicationException($"Coss's {AcquisitionSymbol}-BTC best ask price must be > 0."); }

            if (cossBchabcBtcBestBidPrice >= cossBchabcBtcBestAskPrice) { throw new ApplicationException($"Coss's {AcquisitionSymbol}-BTC bet bid price must be less than its best ask price."); }

            var binanceBchabcBtcBestBidPrice = binanceBchabcBtcOrderBook.BestBid().Price;
            if (binanceBchabcBtcBestBidPrice <= 0) { throw new ApplicationException($"Binance's {AcquisitionSymbol}-BTC best bid price must be > 0."); }

            var binanceBchabcBtcBestAskPrice = binanceBchabcBtcOrderBook.BestAsk().Price;
            if (binanceBchabcBtcBestAskPrice <= 0) { throw new ApplicationException($"Binance's {AcquisitionSymbol}-BTC best ask price must be > 0."); }

            if (binanceBchabcBtcBestBidPrice >= binanceBchabcBtcBestAskPrice) { throw new ApplicationException($"Binance's {AcquisitionSymbol}-BTC bet bid price must be less than its best ask price."); }

            decimal? priceToBid = null;
            var optimumBidPrice = binanceBchabcBtcBestBidPrice * (100.0m - OptimumBidPercentDiff) / 100.0m;
            if (optimumBidPrice > cossBchabcBtcBestBidPrice)
            {                
                priceToBid = MathUtil.ConstrainToMultipleOf(optimumBidPrice, priceTick);
            }
            else
            {
                var tickUpBidPrice = cossBchabcBtcBestBidPrice + priceTick;
                var diff = tickUpBidPrice - binanceBchabcBtcBestBidPrice;
                var ratio = diff / binanceBchabcBtcBestBidPrice;
                var percentDiff = 100.0m * ratio;

                if (percentDiff >= MinBidPercentDiff)
                {
                    priceToBid = tickUpBidPrice;
                }
            }

            if (priceToBid.HasValue)
            {
                const string BaseSymbol = "BTC";
                var quantity = 0.1m;
                var price = priceToBid.Value;

                _log.Info($"About to place a bid on Coss for {quantity} {AcquisitionSymbol} at {price} {BaseSymbol}.");
                try
                {
                    var orderResult = _exchangeClient.BuyLimit(IntegrationNameRes.Coss, AcquisitionSymbol, BaseSymbol, new QuantityAndPrice
                    {
                        Quantity = quantity,
                        Price = price
                    });

                    if (orderResult)
                    {
                        _log.Info($"Successfully placed a bid on Coss for {quantity} {AcquisitionSymbol} at {price} {BaseSymbol}.");
                    }
                    else
                    {
                        _log.Error($"Failed to place a bid on Coss for {quantity} {AcquisitionSymbol} at {price} {BaseSymbol}.");
                    }
                }
                catch(Exception exception)
                {
                    _log.Error($"Failed to place a bid on Coss for {quantity} {AcquisitionSymbol} at {price} {BaseSymbol}.{Environment.NewLine}{exception.Message}");
                    _log.Error(exception);
                }
            }
        }

        private void CancelOpenOrdersForTradingPair(string exchange, string symbol, string baseSymbol)
        {
            var openOrders = GetOpenOrdersForTradingPairV2WithRetries(exchange, symbol, baseSymbol, CachePolicy.ForceRefresh);
            foreach (var openOrder in openOrders?.OpenOrders ?? new List<OpenOrder>())
            {
                CancelOpenOrderWithRetries(exchange, openOrder.OrderId);
            }
        }

        private bool SellLimitWithCompensation(string exchange, string symbol, string baseSymbol, QuantityAndPrice quantityAndPrice, decimal lotSize)
        {
            bool orderResult = false;
            try
            {
                orderResult = _exchangeClient.SellLimit(exchange, symbol, baseSymbol, quantityAndPrice);
                if (orderResult) { return true; }
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                orderResult = false;
            }

            var updatedBalance = GetCossBalanceWithRetries(symbol, CachePolicy.ForceRefresh);
            var updatedAvailable = updatedBalance?.Available ?? 0;

            if (updatedAvailable <= 0) { return false; }

            var quantityNeeded = quantityAndPrice.Quantity * quantityAndPrice.Price * 1.0015m;
            if (quantityNeeded <= updatedAvailable)
            {
                return false;
            }

            var updatedQuantity = MathUtil.ConstrainToMultipleOf(updatedAvailable / (quantityAndPrice.Price * 1.0015m), lotSize);

            return _exchangeClient.SellLimit(exchange, symbol, baseSymbol,
                new QuantityAndPrice
                {
                    Quantity = updatedQuantity,
                    Price = quantityAndPrice.Price
                });
        }

        private decimal GetLotSize(List<TradingPair> tradingPairs, string symbol, string baseSymbol)
        {
            return tradingPairs.SingleOrDefault(item =>
                    string.Equals(item.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(item.Symbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase))
                    ?.LotSize ?? DefaultLotSize;
        }
    }
}
