using cache_lib.Models;
using math_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using task_lib;
using trade_constants;
using trade_model;

namespace coss_arb_lib
{
    public partial class CossArbUtil
    {
        public void AcquireCossV4()
        {
            const string AcquisitionSymbol = "COSS";

            const int MaxPriceDecimals = 8;

            const decimal CossTopParBalance = 25000;
            const decimal CossBottomParBalance = 1500;

            const decimal MaxQuantityToBuy = 2500.0m;

            var maxQuantityToBuyDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "BCHABC", 5000.0m },
                { "TUSD", 10000.0m },
                { "USDT", 10000.0m },
                { "USDC", 10000.0m },
                { "GUSD", 10000.0m }
            };

            const decimal MinQuantityToBuy = 100.0m;

            const decimal MaxQuantityToSell = 2000.0m;
            const decimal MinQuantityToSell = 250.0m;

            const decimal MinPriceGapPercent = 1.5m;
            const decimal MinAskPercentDiff = 1.0m;
            const decimal MinBidPercentDiff = 1.5m;

            const decimal MinEthBalance = 1.0m;

            const decimal MinAltProfitPercentage = 2.0m;
            const decimal OptimumAltProfitPercentage = 15.0m;

            // If we own at least this much TUSD, place a COSS-TUSD bid even if we own more than the CossTopParBalance.
            const decimal usdXHoldingWhereWeIgnoreTopCossPar = 1000.0m;

            decimal ethValue = 0;
            decimal btcValue = 0;
            var valuationDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase);

            OrderBook binanceEthBtcOrderBook = null;

            var valuationTask = LongRunningTask.Run(() =>
            {
                var ethValueContainer = _workflowClient.GetUsdValueV2("ETH", CachePolicy.AllowCache);
                ethValue = ethValueContainer.UsdValue.Value;

                var btcValueContainer = _workflowClient.GetUsdValueV2("BTC", CachePolicy.AllowCache);
                btcValue = btcValueContainer.UsdValue.Value;

                binanceEthBtcOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, "ETH", "BTC", CachePolicy.ForceRefresh);
            });

            foreach (var baseSymbol in new List<string> { "ETH", "BTC", "USDT", "TUSD", "USDC", "GUSD" })
            {
                CancelOpenOrdersForTradingPair(IntegrationNameRes.Coss, AcquisitionSymbol, baseSymbol);
            }

            HoldingInfo balances = GetBalancesWithRetries(IntegrationNameRes.Coss, CachePolicy.ForceRefresh);
            var cossTradingPairs = GetTradingPairsWithRetries(IntegrationNameRes.Coss, CachePolicy.AllowCache);
            var lotSizeDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase);
            var priceTickDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var tradingPair in cossTradingPairs.Where(item => string.Equals(item.Symbol, AcquisitionSymbol, StringComparison.InvariantCultureIgnoreCase)))
            {
                lotSizeDictionary[tradingPair.BaseSymbol] = tradingPair.LotSize ?? DefaultLotSize;
                priceTickDictionary[tradingPair.BaseSymbol] = tradingPair.PriceTick ?? DefaultPriceTick;
            }

            var getCossTradingPair = new Func<string, string, TradingPair>((symbol, baseSymbol) => cossTradingPairs.Single(queryPair =>
                string.Equals(queryPair.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(queryPair.BaseSymbol, baseSymbol, StringComparison.InvariantCultureIgnoreCase)));

            var getLotSize = new Func<string, string, decimal>((symbol, baseSymbol) =>
            {
                var matchingPair = getCossTradingPair(symbol, baseSymbol);
                var lotSize = matchingPair.LotSize;

                if (!lotSize.HasValue) { throw new ApplicationException($"Lot size not found for {symbol}-{baseSymbol}."); }
                if (lotSize.Value <= 0) { throw new ApplicationException($"Lot size for {symbol}-{baseSymbol} must be > 0."); }

                return lotSize.Value;
            });

            var getPriceTick = new Func<string, string, decimal>((symbol, baseSymbol) =>
            {
                var matchingPair = getCossTradingPair(symbol, baseSymbol);
                var priceTick = matchingPair.PriceTick;

                if (!priceTick.HasValue) { throw new ApplicationException($"Price tick not found for {symbol}-{baseSymbol}."); }
                if (priceTick.Value <= 0) { throw new ApplicationException($"Price tick for {symbol}-{baseSymbol} must be > 0."); }

                return priceTick.Value;
            });

            var availableBalanceDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase);
            var cossBalance = balances.GetHoldingForSymbol(AcquisitionSymbol);
            availableBalanceDictionary["COSS"] = cossBalance?.Available ?? 0;

            var ethBalance = balances.GetHoldingForSymbol("ETH");
            var ethBalanceTotal = ethBalance?.Total ?? 0;
            availableBalanceDictionary["ETH"] = ethBalance?.Available ?? 0;

            var tusdBalance = balances.GetHoldingForSymbol("TUSD");
            var tusdBalanceTotal = tusdBalance?.Total ?? 0;
            availableBalanceDictionary["TUSD"] = tusdBalance?.Available ?? 0;

            var usdtBalance = balances.GetHoldingForSymbol("USDT");
            var usdtBalanceTotal = tusdBalance?.Total ?? 0;
            availableBalanceDictionary["USDT"] = usdtBalance?.Available ?? 0;

            var btcBalances = balances.GetHoldingForSymbol("BTC");
            var btcBalanceTotal = btcBalances?.Total ?? 0;
            availableBalanceDictionary["BTC"] = btcBalances?.Available ?? 0;

            var usdcBalance = balances.GetHoldingForSymbol("USDC");
            var usdcBalanceTotal = tusdBalance?.Total ?? 0;
            availableBalanceDictionary["USDC"] = usdcBalance?.Available ?? 0;

            var gusdBalance = balances.GetHoldingForSymbol("GUSD");
            var gusdBalanceTotal = gusdBalance?.Total ?? 0;
            availableBalanceDictionary["GUSD"] = gusdBalance?.Available ?? 0;

            var quantityToSell = cossBalance.Total >= 6000 ? MaxQuantityToSell : MinQuantityToSell;

            var cossEthOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Coss, AcquisitionSymbol, "ETH", CachePolicy.ForceRefresh);
            var cossBtcOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Coss, AcquisitionSymbol, "BTC", CachePolicy.ForceRefresh);

            valuationTask.Wait();
            if (ethValue <= 0) { throw new ApplicationException("ETH value must be > 0"); }
            if (btcValue <= 0) { throw new ApplicationException("BTC value must be > 0"); }

            valuationDictionary["ETH"] = ethValue;
            valuationDictionary["BTC"] = btcValue;

            var ethBtcRatio = ethValue / btcValue;

            var cossCossEthBestBid = cossEthOrderBook.BestBid();
            var cossCossEthBestBidPrice = cossCossEthBestBid.Price;
            if (cossCossEthBestBidPrice <= 0) { throw new ApplicationException("The COSS-ETH best bid price should be > 0."); }

            var cossCossEthBestBidPriceAsBtc = cossCossEthBestBidPrice * ethBtcRatio;

            var cossCossBtcBestBid = cossBtcOrderBook.BestBid();
            var cossCossBtcBestBidPrice = cossCossBtcBestBid.Price;
            if (cossCossBtcBestBidPrice <= 0) { throw new ApplicationException("The COSS-BTC best bid price should be > 0."); }

            var cossBestBidPriceDictionary = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "ETH", cossCossEthBestBidPrice },
                { "BTC", cossCossBtcBestBidPrice },
            };

            var cossBestCossEthBidPriceAsUsd = cossCossEthBestBidPrice * ethValue;
            var cossBestCossBtcBidPriceAsUsd = cossCossBtcBestBidPrice * btcValue;

            var cossCossEthCossBestAsk = cossEthOrderBook.BestAsk(0.01m);
            var cossCossEthBestAskPrice = cossCossEthCossBestAsk.Price;
            if (cossCossEthBestAskPrice <= 0) { throw new ApplicationException("The COSS-ETH best ask price should be > 0."); }

            var cossCossEthBestAskPriceAsBtc = cossCossEthBestAskPrice * ethBtcRatio;

            var cossCossBtcBestAsk = cossBtcOrderBook.BestAsk(0.01m);
            var cossCossBtcBestAskPrice = cossCossBtcBestAsk.Price;
            if (cossCossBtcBestAskPrice <= 0) { throw new ApplicationException("The COSS-BTC best ask price should be > 0."); }

            var cossCossEthBestAskPriceAsUsd = cossCossEthBestAskPrice * ethValue;
            var cossCossBtcBestAskPriceAsUsd = cossCossBtcBestAskPrice * btcValue;

            var cossCossEthPriceGap = cossCossEthBestAskPrice - cossCossEthBestBidPrice;
            var cossCossEthPriceGapRatio = cossCossEthPriceGap / cossCossEthBestBidPrice;
            var cossCossEthPriceGapPercentDiff = 100.0m * cossCossEthPriceGapRatio;

            var cossCossBtcPriceGap = cossCossBtcBestAskPrice - cossCossBtcBestBidPrice;
            var cossCossBtcPriceGapRatio = cossCossBtcPriceGap / cossCossBtcBestBidPrice;
            var cossCossBtcPriceGapPercentDiff = 100.0m * cossCossBtcPriceGapRatio;

            var avgCossCossBtcPrice = DetermineCossBtcValue(
                new CossValuationData
                {
                    CossCossBtcOrderBook = cossBtcOrderBook,
                    CossCossEthOrderBook = cossEthOrderBook,
                    BinanceEthBtcOrderBook = binanceEthBtcOrderBook
                });

            var avgCossCossUsdPrice = avgCossCossBtcPrice * btcValue;

            if (cossBalance.Total < CossTopParBalance)
            {
                bool shouldBuyEthPair = false;
                bool shouldBuyBtcPair = false;
                if (cossBestCossEthBidPriceAsUsd < cossBestCossBtcBidPriceAsUsd)
                {
                    if (cossCossEthPriceGapPercentDiff >= MinPriceGapPercent)
                    {
                        var diffFromAvg = avgCossCossBtcPrice - cossCossEthBestBidPriceAsBtc;
                        var diffRatio = diffFromAvg / avgCossCossBtcPrice;
                        var percentDiff = 100.0m * diffRatio;

                        if (percentDiff >= MinBidPercentDiff)
                        {
                            shouldBuyEthPair = ethBalanceTotal >= MinEthBalance;

                            var acceptableBtcBidPriceAsUsd = (3.0m * cossBestCossEthBidPriceAsUsd + cossCossEthBestAskPriceAsUsd) / 4.0m;
                            if (cossBestCossBtcBidPriceAsUsd < acceptableBtcBidPriceAsUsd && cossCossBtcPriceGapPercentDiff >= MinPriceGapPercent)
                            { shouldBuyBtcPair = true; }
                        }
                    }
                }
                else
                {
                    if (cossCossBtcPriceGapPercentDiff >= MinPriceGapPercent)
                    {
                        var diffFromAvg = avgCossCossBtcPrice - cossCossBtcBestBidPrice;
                        var diffRatio = diffFromAvg / cossCossBtcBestBidPrice;
                        var percentDiff = 100.0m * diffRatio;

                        if (percentDiff >= MinBidPercentDiff)
                        {
                            shouldBuyBtcPair = true;
                            var acceptableEthBidPriceAsUsd = (3.0m * cossBestCossBtcBidPriceAsUsd + cossCossBtcBestAskPriceAsUsd) / 4.0m;

                            if (cossBestCossEthBidPriceAsUsd < acceptableEthBidPriceAsUsd && cossCossEthPriceGapPercentDiff >= MinPriceGapPercent)
                            { shouldBuyEthPair = true; }
                        }
                    }
                }

                var shouldBuyCombos = new List<(string BaseSymbol, bool ShouldBuy)>
                {
                    ("ETH", shouldBuyEthPair),
                    ("BTC", shouldBuyBtcPair),
                };

                foreach (var shouldBuyCombo in shouldBuyCombos)
                {
                    if (!shouldBuyCombo.ShouldBuy) { continue; }

                    var baseSymbol = shouldBuyCombo.BaseSymbol;
                    var lotSize = lotSizeDictionary[baseSymbol];
                    var priceTick = priceTickDictionary[baseSymbol];
                    var availableBalance = availableBalanceDictionary[baseSymbol];

                    var bestBidPrice = cossBestBidPriceDictionary[baseSymbol];
                    var price = bestBidPrice + priceTick;

                    decimal? quantityToBid = null;
                    if (price * MaxQuantityToBuy * 1.0015m <= availableBalanceDictionary[baseSymbol])
                    {
                        quantityToBid = MaxQuantityToBuy;
                        var valueAvailable = availableBalanceDictionary[baseSymbol] * valuationDictionary[baseSymbol];
                        if(valueAvailable < 400)
                        {
                            quantityToBid /= 2.0m;
                        }
                        if (valueAvailable >= 500.0m)
                        {
                            quantityToBid *= 2.0m;
                        }
                    }
                    else if (availableBalanceDictionary[baseSymbol] > 0)
                    {
                        var potentialQuantity = availableBalanceDictionary[baseSymbol] / (price * 1.0015m);
                        if (potentialQuantity >= MinQuantityToBuy)
                        {
                            quantityToBid = potentialQuantity;
                        }
                    }

                    if (quantityToBid.HasValue)
                    {
                        var quantity = MathUtil.ConstrainToMultipleOf(quantityToBid.Value, lotSizeDictionary[baseSymbol]);
                        _log.Info($"About to place a limit bid for {quantity} {AcquisitionSymbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.");

                        try
                        {
                            var limitResult = _exchangeClient.BuyLimit(IntegrationNameRes.Coss, AcquisitionSymbol, baseSymbol,
                                new QuantityAndPrice
                                {
                                    Price = price,
                                    Quantity = quantity
                                });

                            if (limitResult)
                            {
                                availableBalanceDictionary[baseSymbol] = availableBalanceDictionary[baseSymbol] - quantity * price * 1.0m;
                                _log.Info($"Successfully placed a limit bid for {quantity} {AcquisitionSymbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.");
                            }
                            else
                            {
                                _log.Error($"Failed to limit bid for {quantity} {AcquisitionSymbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.");
                            }
                        }
                        catch (Exception exception)
                        {
                            _log.Error($"Failed to limit bid for {quantity} {AcquisitionSymbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.{Environment.NewLine}{exception.Message}");
                            _log.Error(exception);
                        }
                    }
                    else
                    {
                        _log.Info($"We do not have enough {baseSymbol} to place a {AcquisitionSymbol}-{baseSymbol} bid at the minimum quantity of {MinQuantityToBuy}.");
                    }
                }
            }

            if (cossBalance.Total > CossBottomParBalance)
            {
                bool shouldSellEthPair = false;
                bool shouldSellBtcPair = false;
                if (cossCossEthBestAskPriceAsUsd > cossCossBtcBestAskPriceAsUsd)
                {
                    shouldSellEthPair = true;

                    var acceptableAskAsUsd = (3.0m * cossCossEthBestAskPriceAsUsd + cossBestCossEthBidPriceAsUsd) / 4.0m;

                    if (cossCossBtcBestAskPriceAsUsd >= acceptableAskAsUsd)
                    { shouldSellBtcPair = true; }
                }
                else
                {
                    shouldSellBtcPair = true;

                    var acceptableAskAsUsd = (3.0m * cossCossBtcBestAskPriceAsUsd + cossBestCossBtcBidPriceAsUsd) / 4.0m;

                    if (cossCossEthBestAskPriceAsUsd >= acceptableAskAsUsd)
                    { shouldSellEthPair = true; }
                }

                if (shouldSellEthPair)
                {
                    if (quantityToSell >= availableBalanceDictionary["COSS"] * 0.75m)
                    {
                        quantityToSell = availableBalanceDictionary["COSS"] * 0.75m;
                    }

                    var baseSymbol = "ETH";
                    var price = cossCossEthBestAskPrice - priceTickDictionary[baseSymbol];
                    var quantity = MathUtil.ConstrainToMultipleOf(quantityToSell, lotSizeDictionary[baseSymbol]);

                    _log.Info($"About to place a limit ask for {quantity} {AcquisitionSymbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.");

                    try
                    {
                        var sellLimitResult = _exchangeClient.SellLimit(IntegrationNameRes.Coss, AcquisitionSymbol, baseSymbol,
                            new QuantityAndPrice
                            {
                                Price = price,
                                Quantity = quantity
                            });

                        if (sellLimitResult)
                        {
                            availableBalanceDictionary["COSS"] -= quantity * 1.01m;
                            _log.Info($"Successfully placed a limit ask for {quantity} {AcquisitionSymbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.");
                        }
                        else
                        {
                            _log.Error($"Failed to limit ask for {quantity} {AcquisitionSymbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to limit ask for {quantity} {AcquisitionSymbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }

                if (shouldSellBtcPair)
                {
                    var baseSymbol = "BTC";
                    var price = cossCossBtcBestAskPrice - priceTickDictionary[baseSymbol];
                    var quantity = MathUtil.ConstrainToMultipleOf(quantityToSell, lotSizeDictionary[baseSymbol]);

                    _log.Info($"About to place a limit ask for {quantity} {AcquisitionSymbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.");

                    try
                    {
                        var sellLimitResult = _exchangeClient.SellLimit(IntegrationNameRes.Coss, AcquisitionSymbol, baseSymbol,
                            new QuantityAndPrice
                            {
                                Price = price,
                                Quantity = quantity
                            });

                        if (sellLimitResult)
                        {
                            availableBalanceDictionary["COSS"] -= quantity * 1.01m;
                            _log.Info($"Successfully placed a limit ask for {quantity} {AcquisitionSymbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.");
                        }
                        else
                        {
                            _log.Error($"Failed to limit ask for {quantity} {AcquisitionSymbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.");
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to limit ask for {quantity} {AcquisitionSymbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);
                    }
                }
            }

            var altBases = new List<string>
            {
                "TUSD",
                "GUSD", 
                "USDC",
                "USDT"                
            };

            for (var i = 0; i < altBases.Count; i++)
            {
                var altBase = altBases[i];
                var altTick = priceTickDictionary[altBase];

                var cossAltTradingPair = cossTradingPairs.SingleOrDefault(item => string.Equals(item.Symbol, "COSS", StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(item.BaseSymbol, altBase, StringComparison.InvariantCultureIgnoreCase));

                OrderBook binanceAltBtcOrderBook = null;

                if (string.Equals(altBase, "USDT", StringComparison.InvariantCultureIgnoreCase)
                    || string.Equals(altBase, "USDC", StringComparison.InvariantCultureIgnoreCase))
                {
                    binanceAltBtcOrderBook = InvertOrderBook(GetOrderBookWithRetries(IntegrationNameRes.Binance, "BTC", altBase, CachePolicy.AllowCache));
                }
                // Binance doesn't have GUSD, so let's grab it from HitBTC instead.
                else if (string.Equals(altBase, "GUSD", StringComparison.InvariantCultureIgnoreCase))
                {
                    binanceAltBtcOrderBook = InvertOrderBook(GetOrderBookWithRetries(IntegrationNameRes.HitBtc, "BTC", "GUSD", CachePolicy.AllowCache));
                }
                else
                {
                    binanceAltBtcOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Binance, altBase, "BTC", CachePolicy.AllowCache);
                }
                
                var cossCossAltOrderBook = GetOrderBookWithRetries(IntegrationNameRes.Coss, AcquisitionSymbol, altBase, CachePolicy.ForceRefresh);
                if (cossCossAltOrderBook != null && binanceAltBtcOrderBook != null && binanceAltBtcOrderBook.Bids.Any() && binanceAltBtcOrderBook.Asks.Any())
                {
                    var binanceAltBtcBestAsk = binanceAltBtcOrderBook.BestAsk(0.01m);
                    var binanceAltBtcBestAskPrice = binanceAltBtcBestAsk.Price;
                    var binanceAltBtcBestAskPriceAsUsd = binanceAltBtcBestAskPrice * btcValue;

                    var binanceAltBtcBestBid = binanceAltBtcOrderBook.BestBid();
                    var binanceAltBtcBestBidPrice = binanceAltBtcBestBid.Price;
                    var binanceAltBtcBestBidPriceAsUsd = binanceAltBtcBestBidPrice * btcValue;

                    var cossCossAltBestAsk = cossCossAltOrderBook.BestAsk(0.01m);
                    var cossCossAltBestAskPrice = cossCossAltBestAsk.Price;
                    var cossCossAltBestBid = cossCossAltOrderBook.BestBid();
                    var cossCossAltBestBidPrice = cossCossAltBestBid?.Price ?? 0.000000001m;

                    var didWePlaceAnInstantAltAsk = false;
                    var cossCossAltBestBidPriceAsBtc = cossCossAltBestBidPrice * binanceAltBtcBestBidPrice;

                    if (string.Equals(altBase, "TUSD", StringComparison.InvariantCultureIgnoreCase)
                        || string.Equals(altBase, "USDT", StringComparison.InvariantCultureIgnoreCase)
                        || string.Equals(altBase, "USDC", StringComparison.InvariantCultureIgnoreCase)
                        || string.Equals(altBase, "GUSD", StringComparison.InvariantCultureIgnoreCase))
                    {
                        
                        if (cossCossAltBestBidPriceAsBtc >= cossCossBtcBestAskPrice
                            && cossCossAltBestBidPriceAsBtc >= cossCossEthBestAskPriceAsBtc)
                        {
                            var price = cossCossAltBestBidPrice;

                            var quantity = cossCossAltBestBid.Quantity;
                            if (quantity > availableBalanceDictionary["COSS"])
                            {
                                quantity = availableBalanceDictionary["COSS"];
                            }

                            // TODO: Remove this safeguard after some further testing.
                            if (quantity > 10000) { quantity = 10000; }

                            var shouldCancelAfterPlacingOrder = false;
                            quantity = MathUtil.ConstrainToMultipleOf(quantity, getLotSize("COSS", altBase));
                            if (cossAltTradingPair != null && cossAltTradingPair.MinimumTradeQuantity.HasValue
                                && quantity < cossAltTradingPair.MinimumTradeQuantity.Value)
                            {
                                quantity = cossAltTradingPair.MinimumTradeQuantity.Value;
                                shouldCancelAfterPlacingOrder = true;
                            }

                            if (cossAltTradingPair != null && cossAltTradingPair.MinimumTradeBaseSymbolValue.HasValue
                                && quantity * price < cossAltTradingPair.MinimumTradeBaseSymbolValue)
                            {
                                quantity = MathUtil.ConstrainToMultipleOf(1.001m * cossAltTradingPair.MinimumTradeBaseSymbolValue.Value / price, getLotSize("COSS", altBase));
                                shouldCancelAfterPlacingOrder = true;
                            }

                            didWePlaceAnInstantAltAsk = true;

                            _log.Info($"About to place an ask on Coss for {quantity} COSS at {price} {altBase}.");
                            try
                            {
                                var orderResult = _exchangeClient.SellLimitV2(IntegrationNameRes.Coss, "COSS", altBase, new QuantityAndPrice
                                {
                                    Price = price,
                                    Quantity = quantity
                                });

                                if (orderResult != null && orderResult.WasSuccessful)
                                {
                                    _log.Info($"Successfully placed an ask on Coss for {quantity} COSS at {price} {altBase}.");

                                    if (shouldCancelAfterPlacingOrder && !string.IsNullOrWhiteSpace(orderResult.OrderId))
                                    {
                                        // Coss prohibits cancelling an order within 10 seconds of creating it.
                                        Thread.Sleep(TimeSpan.FromSeconds(11));
                                        _exchangeClient.CancelOrder(IntegrationNameRes.Coss, orderResult.OrderId);
                                    }
                                }
                                else
                                {
                                    _log.Info($"Failed to place an ask on Coss for {quantity} COSS at {price} {altBase}.{Environment.NewLine}{orderResult?.FailureReason ?? string.Empty}");
                                }
                            }
                            catch (Exception exception)
                            {
                                _log.Info($"Failed to place an ask on Coss for {quantity} COSS at {price} {altBase}.{Environment.NewLine}{exception.Message}");
                                _log.Error(exception);
                            }
                        }

                        // instant buy
                        var cossCossAltBestAskPriceAsBtc = cossCossAltBestAskPrice * binanceAltBtcBestAskPrice;
                        var cossCossAltBestAskPriceAsEth = cossCossAltBestAskPrice * binanceAltBtcBestAskPrice * ethBtcRatio;
                        if (cossCossAltBestAskPriceAsBtc < cossCossBtcBestBidPrice / 1.01m
                            && cossCossAltBestAskPriceAsEth < cossCossEthBestBidPrice / 1.01m)
                        {
                            var price = cossCossAltBestAsk.Price;

                            var altBaseAvailable = availableBalanceDictionary[altBase];
                            var maxPotentialQuantity = altBaseAvailable / (price * 1.01m);

                            var quantity = cossCossAltBestAsk.Quantity > maxPotentialQuantity
                                ? MathUtil.ConstrainToMultipleOf(maxPotentialQuantity, lotSizeDictionary[altBase])
                                : cossCossAltBestAsk.Quantity;

                            try
                            {
                                _log.Info($"About to place a bid on Coss for {quantity} {AcquisitionSymbol} at {price} {altBase}.");

                                var orderResult = _exchangeClient.BuyLimitV2(IntegrationNameRes.Coss, AcquisitionSymbol, altBase, new QuantityAndPrice
                                {
                                    Quantity = quantity,
                                    Price = price
                                });

                                if (orderResult.WasSuccessful)
                                {
                                    _log.Info($"Successfully placed a bid on Coss for {quantity} {AcquisitionSymbol} at {price} {altBase}.");
                                    availableBalanceDictionary[altBase] -= quantity * price * 1.01m;
                                    availableBalanceDictionary[AcquisitionSymbol] += quantity;

                                    var matchingOrder = cossCossAltOrderBook.Asks.FirstOrDefault(item => item.Price == cossCossAltBestAsk.Price && item.Quantity == cossCossAltBestAsk.Quantity);
                                    if (matchingOrder != null)
                                    {
                                        cossCossAltOrderBook.Asks.Remove(matchingOrder);
                                        cossCossAltBestAsk = cossCossAltOrderBook.BestAsk(0.01m);
                                        cossCossAltBestAskPrice = cossCossAltBestAsk.Price;
                                    }
                                }
                                else
                                {
                                    _log.Error($"Failed to place a bid on Coss for {quantity} {AcquisitionSymbol} at {price} {altBase}.");
                                }
                            }
                            catch (Exception exception)
                            {
                                _log.Error($"Failed to place a bid on Coss for {quantity} {AcquisitionSymbol} at {price} {altBase}.{Environment.NewLine}{exception.Message}");
                                _log.Error(exception);
                            }
                        }
                    }


                    var cossCossAltPotentialAskPrice = cossCossAltBestAskPrice - altTick;
                    var cossCossAltPotentialAskPriceAsBtc = cossCossAltPotentialAskPrice * binanceAltBtcBestBidPrice;
                    var cossCossAltPotentialAskPriceAsEth = cossCossAltPotentialAskPriceAsBtc / ethBtcRatio;

                    var existingPriceGap = cossCossAltBestAskPrice - cossCossAltBestBidPrice;
                    var existingPriceGapRatio = existingPriceGap / cossCossAltBestBidPrice;
                    var existingPriceGapPercentDiff = 100.0m * existingPriceGapRatio;

                    decimal? cossCossAltPriceToBid = null;

                    var optimalCossAltBidAsBtc = avgCossCossBtcPrice * (1.0m - OptimumAltProfitPercentage / 100.0m);
                    var optimalCossAltBidAsAlt = MathUtil.Truncate(optimalCossAltBidAsBtc / binanceAltBtcBestBidPrice, MaxPriceDecimals);

                    if (optimalCossAltBidAsAlt > cossCossAltBestBidPrice)
                    {
                        var priceGapForOptimal = cossCossAltBestAskPrice - optimalCossAltBidAsAlt;
                        var priceGapForOptimalRatio = priceGapForOptimal / optimalCossAltBidAsAlt;
                        var priceGapForOptimalPercent = 100.0m * priceGapForOptimalRatio;
                        if (priceGapForOptimalPercent >= MinPriceGapPercent)
                        {
                            cossCossAltPriceToBid = optimalCossAltBidAsAlt;
                        }
                    }

                    if (!cossCossAltPriceToBid.HasValue)
                    {
                        var cossCossAltTickUpBidPrice = cossCossAltBestBidPrice + altTick;
                        var cossCossAltTickUpBidPriceAsBtc = cossCossAltTickUpBidPrice * binanceAltBtcBestAskPrice;

                        var potentialBidAsBtcDiffFromAverage = avgCossCossBtcPrice - cossCossAltTickUpBidPriceAsBtc;
                        var potentialBidAsBtcProfitRatio = potentialBidAsBtcDiffFromAverage / cossCossAltTickUpBidPriceAsBtc;
                        var potentialBidAsBtcProfitPercent = 100.0m * potentialBidAsBtcProfitRatio;

                        if (potentialBidAsBtcProfitPercent >= MinAltProfitPercentage)
                        {
                            cossCossAltPriceToBid = cossCossAltTickUpBidPrice;
                        }
                    }

                    if (cossBalance.Total >= CossBottomParBalance)
                    {
                        var diffFromAvg = cossCossAltPotentialAskPriceAsBtc - avgCossCossBtcPrice;
                        var diffFromAvgRatio = diffFromAvg / avgCossCossBtcPrice;
                        var diffFromAvgPercent = 100.0m * diffFromAvgRatio;

                        var shouldSellAltPair = !didWePlaceAnInstantAltAsk && diffFromAvgPercent >= MinAskPercentDiff;

                        var diffFromBestBtcBidPrice = cossCossAltPotentialAskPriceAsBtc - cossCossBtcBestBidPrice;
                        var diffFromBestBtcBidPriceRatio = diffFromBestBtcBidPrice / cossCossBtcBestBidPrice;
                        var diffFromBestBtcBidPricePercent = 100.0m * diffFromBestBtcBidPriceRatio;

                        if (diffFromBestBtcBidPricePercent < 2.5m) { shouldSellAltPair = false; }

                        var diffFromBestEthBidPrice = cossCossAltPotentialAskPriceAsEth - cossCossEthBestBidPrice;
                        var diffFromBestEthBidPriceRatio = diffFromBestEthBidPrice / cossCossEthBestBidPrice;
                        var diffFromBestEthBidPricePercent = 100.0m * diffFromBestEthBidPriceRatio;

                        if (diffFromBestEthBidPricePercent < 2.5m) { shouldSellAltPair = false; }

                        if (shouldSellAltPair && availableBalanceDictionary["COSS"] > 0 && quantityToSell > 0)
                        {
                            var baseSymbol = altBase;
                            var price = cossCossAltPotentialAskPrice;

                            _log.Info($"About to place a limit ask for {quantityToSell} {AcquisitionSymbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.");

                            var quantity = quantityToSell;
                            var availableCossForThisRound = i == 0 ? availableBalanceDictionary["COSS"] * 2.0m / 3.0m : availableBalanceDictionary["COSS"];
                            if (availableCossForThisRound < quantityToSell)
                            {
                                quantity = MathUtil.ConstrainToMultipleOf(availableCossForThisRound * 0.9m, 0.00000001m);
                            }

                            try
                            {
                                var sellLimitResult = _exchangeClient.SellLimitV2(IntegrationNameRes.Coss, AcquisitionSymbol, baseSymbol,
                                    new QuantityAndPrice
                                    {
                                        Price = price,
                                        Quantity = quantity
                                    });

                                //todo: insufficient funds isn't being marked as a failure.

                                if (sellLimitResult.WasSuccessful)
                                {
                                    availableBalanceDictionary["COSS"] -= quantity;
                                    _log.Info($"Successfully placed a limit ask for {quantityToSell} {AcquisitionSymbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.");
                                }
                                else
                                {
                                    _log.Error($"Failed to limit ask for {quantityToSell} {AcquisitionSymbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.");
                                }
                            }
                            catch (Exception exception)
                            {
                                _log.Error($"Failed to limit ask for {quantityToSell} {AcquisitionSymbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.{Environment.NewLine}{exception.Message}");
                                _log.Error(exception);
                            }
                        }
                    }

                    var doParsAllowBid = (cossBalance.Total <= CossTopParBalance)
                        || (string.Equals(altBase, "TUSD", StringComparison.InvariantCultureIgnoreCase) && tusdBalanceTotal >= usdXHoldingWhereWeIgnoreTopCossPar);

                    if (doParsAllowBid)
                    {
                        var shouldBuyAltPair = cossCossAltPriceToBid.HasValue; // diffFromAvgPercent >= MinBidPercentDiff;
                        if (shouldBuyAltPair)
                        {
                            var baseSymbol = altBase;                            
                            var price = MathUtil.ConstrainToMultipleOf(cossCossAltPriceToBid.Value, getPriceTick("COSS", altBase));
                            var availableAlt = availableBalanceDictionary[altBase];

                            var effectiveMaxQuantityToBuy = maxQuantityToBuyDictionary.ContainsKey(altBase)
                                ? maxQuantityToBuyDictionary[altBase]
                                : MaxQuantityToBuy;

                            var quantity = MathUtil.ConstrainToMultipleOf(availableAlt / (price * 1.01m), getLotSize("COSS", altBase));
                            if (quantity > effectiveMaxQuantityToBuy) { quantity = effectiveMaxQuantityToBuy; }

                            if (quantity > 10)
                            {
                                _log.Info($"About to place a limit bid for {quantity} {AcquisitionSymbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.");

                                try
                                {
                                    var limitResult = _exchangeClient.BuyLimitV2(IntegrationNameRes.Coss, AcquisitionSymbol, baseSymbol,
                                        new QuantityAndPrice
                                        {
                                            Price = price,
                                            Quantity = quantity
                                        });

                                    if (limitResult.WasSuccessful)
                                    {
                                        _log.Info($"Successfully placed a limit bid for {quantity} {AcquisitionSymbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.");
                                    }
                                    else
                                    {
                                        _log.Error($"Failed to limit bid for {quantity} {AcquisitionSymbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.");
                                    }
                                }
                                catch (Exception exception)
                                {
                                    _log.Error($"Failed to limit bid for {quantity} {AcquisitionSymbol} at {price} {baseSymbol} on {IntegrationNameRes.Coss}.{Environment.NewLine}{exception.Message}");
                                    _log.Error(exception);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
