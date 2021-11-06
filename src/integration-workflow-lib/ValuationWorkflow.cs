using cache_lib.Models;
using cryptocompare_client_lib;
using log_lib;
using res_util_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using trade_model;
using trade_res;
using exchange_client_lib;
using currency_converter_lib;
using trade_constants;
using task_lib;

namespace integration_workflow_lib
{
    public class ValuationWorkflow : IValuationWorkflow
    {
        private readonly IExchangeClient _exchangeClient;
        private readonly ICurrencyConverterIntegration _currencyConverterIntegration;
        private readonly ICryptoCompareClient _cryptoCompareClient;
        private readonly ILogRepo _log;

        public ValuationWorkflow(
            IExchangeClient exchangeClient,
            ICurrencyConverterIntegration currencyConverterIntegration,
            ICryptoCompareClient cryptoCompareClient,
            ILogRepo log)
        {
            _exchangeClient = exchangeClient;
            _currencyConverterIntegration = currencyConverterIntegration;
            _cryptoCompareClient = cryptoCompareClient;
            _log = log;
        }

        private static object Locker = new object();

        private static Dictionary<string, decimal> ValuationDictionary
        {
            get
            {
                lock (_valuationDictionaryLocker)
                {
                    if (_valuationDictionary == null) { return null; }
                    var dict = new Dictionary<string, decimal>();
                    foreach (var key in _valuationDictionary.Keys)
                    {
                        dict[key] = _valuationDictionary[key];
                    }

                    return dict;
                }
            }
        }

        private static object _valuationDictionaryLocker = new object();
        private static Dictionary<string, decimal> _valuationDictionary = new Dictionary<string, decimal>();
        private static bool _hasBeenRunBefore = false;

        public Dictionary<string, decimal> GetValuationDictionary(CachePolicy cachePolicy)
        {
            FillValuationDictionary(cachePolicy);

            return ValuationDictionary;
        }
        
        public decimal? GetValue(string symbol, CachePolicy cachePolicy)
        {
            return GetUsdValueV2(symbol, cachePolicy).Data;
        }

        public AsOfWrapper<decimal?> GetUsdValueV2(string symbol, CachePolicy cachePolicy)
        {
            var unitSymbols = new List<string> { "USD" };
            if (unitSymbols.Any(item => string.Equals(item, symbol, StringComparison.InvariantCultureIgnoreCase)))
            {
                return new AsOfWrapper<decimal?> { Data = 1, AsOfUtc = DateTime.UtcNow };
            }

            var forexSymbols = new List<string> { "GBP", "EUR", "JPY", "RUR" };
            if (forexSymbols.Contains(symbol.ToUpper()))
            {
                var result = _currencyConverterIntegration.GetConversionRate(symbol, cachePolicy);
                return new AsOfWrapper<decimal?> { Data = result.Data, AsOfUtc = result.AsOfUtc };
            }

            if (string.Equals(symbol, "TUSD", StringComparison.InvariantCultureIgnoreCase))
            {
                return DetermineBinanceStableCoinValue(symbol, cachePolicy);
            }

            if (string.Equals(symbol, "USDT", StringComparison.InvariantCultureIgnoreCase))
            {
                return DetermineBinanceStableCoinValue(symbol, cachePolicy);
            }

            if (string.Equals(symbol, "USDC", StringComparison.InvariantCultureIgnoreCase))
            {
                return DetermineBinanceStableCoinValue(symbol, cachePolicy);
            }

            if (string.Equals(symbol, "COSS", StringComparison.InvariantCultureIgnoreCase))
            {
                return DetermineCossValue(cachePolicy);
            }

            if (string.Equals(symbol, "XDCE", StringComparison.InvariantCultureIgnoreCase))
            {
                return DetermineXdceValue(cachePolicy);
            }

            if (CryptoCompareSymbols.Any(item => string.Equals(item, symbol, StringComparison.InvariantCultureIgnoreCase)))
            {
                var result = _cryptoCompareClient.GetUsdValueV2(symbol, cachePolicy);
                if (result.UsdValue.HasValue && result.UsdValue.Value > 0)
                {
                    lock (_valuationDictionaryLocker)
                    {
                        _valuationDictionary[symbol] = result.UsdValue.Value;
                    }
                }

                // return result;
                return new AsOfWrapper<decimal?>
                {
                    AsOfUtc = result.AsOfUtc,
                    Data = result.UsdValue
                };
            }

            return new AsOfWrapper<decimal?>();
        }

private AsOfWrapper<decimal?> DetermineCossValue(CachePolicy cachePolicy)
{
    OrderBook binanceEthBtcOrderBook = null;
    (decimal? UsdValue, DateTime? AsOfUtc) btcUsdValue = ((decimal?)null, (DateTime?)null);

    var binanceTask = LongRunningTask.Run(() =>
    {
        binanceEthBtcOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, "ETH", "BTC", cachePolicy); 
    });

    var cryptoCompareTask = LongRunningTask.Run(() =>
    {
        btcUsdValue = _cryptoCompareClient.GetUsdValueV2("BTC", cachePolicy);
    });

    var cossCossEthOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Coss, "COSS", "ETH", cachePolicy);
    var cossCossBtcOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Coss, "COSS", "BTC", cachePolicy);

    binanceTask.Wait();
    cryptoCompareTask.Wait();

    var binanceEthBtcBestAskPrice = binanceEthBtcOrderBook.BestAsk().Price;
    var binanceEthBtcBestBidPrice = binanceEthBtcOrderBook.BestBid().Price;

    var ethBtcRatio = new List<decimal> { binanceEthBtcBestAskPrice, binanceEthBtcBestBidPrice }.Average();

    decimal totalWeight = 0;
    decimal totalTop = 0;

    const decimal MinimumImportantQuantity = 1.0m;

    var btcAsks = cossCossBtcOrderBook.Asks.Where(item => item.Quantity >= MinimumImportantQuantity).OrderBy(item => item.Price).Take(3).ToList();
    for (var i = 0; i < btcAsks.Count; i++)
    {
        var order = btcAsks[i];
        var quantitySqrt = (decimal)Math.Sqrt((double)order.Quantity);
        var weight = quantitySqrt / ((i + 1) * (i + 1));

        totalTop += order.Price * weight;
        totalWeight += weight;
    }

    var btcBids = cossCossBtcOrderBook.Bids.Where(item => item.Quantity >= MinimumImportantQuantity).OrderByDescending(item => item.Price).Take(3).ToList();
    for (var i = 0; i < btcBids.Count; i++)
    {
        var order = btcBids[i];
        var quantitySqrt = (decimal)Math.Sqrt((double)order.Quantity);
        var weight = quantitySqrt / ((i + 1) * (i + 1));

        totalTop += order.Price * weight;
        totalWeight += weight;
    }

    var ethAsks = cossCossEthOrderBook.Asks.Where(item => item.Quantity >= MinimumImportantQuantity).OrderBy(item => item.Price).Take(3).ToList();
    for (var i = 0; i < ethAsks.Count; i++)
    {
        var order = ethAsks[i];
        var quantitySqrt = (decimal)Math.Sqrt((double)order.Quantity);
        var weight = quantitySqrt / ((i + 1) * (i + 1));

        totalTop += order.Price * weight * ethBtcRatio;
        totalWeight += weight;
    }

    var ethBids = cossCossEthOrderBook.Bids.Where(item => item.Quantity >= MinimumImportantQuantity).OrderByDescending(item => item.Price).Take(3).ToList();
    for (var i = 0; i < ethBids.Count; i++)
    {
        var order = ethBids[i];
        var quantitySqrt = (decimal)Math.Sqrt((double)order.Quantity);
        var weight = quantitySqrt / ((i + 1) * (i + 1));

        totalTop += order.Price * weight * ethBtcRatio;
        totalWeight += weight;
    }

    var cossBtcValuation = totalTop / totalWeight;
    var cossUsdValuation = cossBtcValuation * btcUsdValue.UsdValue.Value;

    var lastDate = new List<DateTime>
    {
        cossCossEthOrderBook.AsOf.Value,
        cossCossBtcOrderBook.AsOf.Value,
        btcUsdValue.AsOfUtc.Value,
        binanceEthBtcOrderBook.AsOf.Value
    }
    .OrderBy(item => item)
    .First();

    return new AsOfWrapper<decimal?> { Data = cossUsdValuation, AsOfUtc = lastDate };
}

        public AsOfWrapper<decimal?> DetermineBinanceStableCoinValue(string stableCoin, CachePolicy cachePolicy)
        {
            const decimal MinimumTusdImportantQuantity = 0.1m;

            var reverseSymbols = new List<string> { "USDC", "USDT" };
            var isReverseSymbol = reverseSymbols.Any(queryReverseSymbol => string.Equals(stableCoin, queryReverseSymbol, StringComparison.InvariantCultureIgnoreCase));
            var orderBookTask = LongRunningTask.Run(() =>
            {
                var book = isReverseSymbol
                    ? _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, "BTC", stableCoin, cachePolicy)
                    : _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, stableCoin, "BTC", cachePolicy);

                return book;
            });

            var btcResult = _cryptoCompareClient.GetUsdValueV2("BTC", cachePolicy);

            var orderBook = orderBookTask.Result;
            var bestAskPrice = orderBook.BestAsk(MinimumTusdImportantQuantity).Price;

            if (bestAskPrice <= 0) { throw new ApplicationException("Best ask price should be > 0."); }

            var bestBidPrice = orderBook.BestBid(MinimumTusdImportantQuantity).Price;
            if (bestBidPrice <= 0) { throw new ApplicationException("Best bid price should be > 0."); }

            if (isReverseSymbol)
            {
                var averageBtcStable = (bestAskPrice + bestBidPrice) / 2.0m;
                var stableUsdValue = btcResult.UsdValue.Value / averageBtcStable;

                return new AsOfWrapper<decimal?> { Data = stableUsdValue, AsOfUtc = btcResult.AsOfUtc };
            }

            var averageTusdBtc = (bestAskPrice + bestBidPrice) / 2.0m;
            var tusdUsdValue = averageTusdBtc * btcResult.UsdValue.Value;

            return new AsOfWrapper<decimal?> { Data = tusdUsdValue, AsOfUtc = btcResult.AsOfUtc };
        }

        public AsOfWrapper<decimal?> DetermineXdceValue(CachePolicy cachePolicy)
        {
            const string CossSymbol = "XDCE";

            OrderBook binanceEthBtcOrderBook = null;
            (decimal? UsdValue, DateTime? AsOfUtc) btcUsdValue = ((decimal?)null, (DateTime?)null);

            var binanceTask = LongRunningTask.Run(() =>
            {
                binanceEthBtcOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, "ETH", "BTC", cachePolicy);
            });

            var cryptoCompareTask = LongRunningTask.Run(() =>
            {
                btcUsdValue = _cryptoCompareClient.GetUsdValueV2("BTC", cachePolicy);
            });

            var cossCossEthOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Coss, CossSymbol, "ETH", cachePolicy);
            var cossCossBtcOrderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Coss, CossSymbol, "BTC", cachePolicy);

            binanceTask.Wait();
            cryptoCompareTask.Wait();

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

            var cossBtcValuation = totalTop / totalWeight;
            var cossUsdValuation = cossBtcValuation * btcUsdValue.UsdValue.Value;

            var lastDate = new List<DateTime>
            {
                cossCossEthOrderBook.AsOf.Value,
                cossCossBtcOrderBook.AsOf.Value,
                btcUsdValue.AsOfUtc.Value,
                binanceEthBtcOrderBook.AsOf.Value
            }
            .OrderBy(item => item)
            .First();

            return new AsOfWrapper<decimal?> { Data = cossUsdValuation, AsOfUtc = lastDate };
        }

        private void FillValuationDictionary(CachePolicy cachePolicy)
        {
            lock (Locker)
            {
                if (!_hasBeenRunBefore)
                {
                    _hasBeenRunBefore = true;
                    FillValuationDictionaryUnwrapped(CachePolicy.OnlyUseCache);                    
                }

                lock (IsWorkingCheckLock)
                {
                    if (_isWorking) { return; }
                }

                var task = new Task(() => FillValuationDictionaryUnwrapped(cachePolicy), TaskCreationOptions.LongRunning);
                task.Start();
            }
        }

        private static object IsWorkingCheckLock = new object();
        private static bool _isWorking = false;
        private void FillValuationDictionaryUnwrapped(CachePolicy cachePolicy)
        {
            lock (IsWorkingCheckLock)
            {
                if (_isWorking) { return; }
                _isWorking = true;
            }

            try
            {
                lock (_valuationDictionaryLocker)
                {
                    _valuationDictionary["USD"] = 1;
                    // _valuationDictionary["USDC"] = 1;
                    // _valuationDictionary["USDT"] = 1;
                    // _valuationDictionary["TUSD"] = 1;
                }

                // tokens not yet on cryptocompare (as of 2018-05-05)
                // CVT, SCC                
                var symbolsToCheck = ResUtil.Get<List<string>>("cryptocompare-symbols.json", typeof(TradeResDummy).Assembly)
                    .Distinct().OrderBy(item => item).ToList();

                // symbolsToCheck.Insert(0, "0xBTC");

                var failureSymbols = new List<string>();
                foreach (var symbol in symbolsToCheck)
                {
                    try
                    {
                        var prices = _cryptoCompareClient.GetPrices(symbol, cachePolicy);
                        if (prices == null || !prices.Keys.Any())
                        {
                            throw new ApplicationException($"Crypto Compare didn't return any prices for \"{symbol}\".");
                        }

                        var baseSymbols = new List<string> { "USD", "TUSD", "USDT", "BTC", "ETH" };
                        foreach (var baseSymbol in baseSymbols)
                        {
                            lock (_valuationDictionaryLocker)
                            {
                                if (prices.ContainsKey(baseSymbol) && _valuationDictionary.ContainsKey(baseSymbol))
                                {
                                    _valuationDictionary[symbol] = prices[baseSymbol] * _valuationDictionary[baseSymbol];
                                    continue;
                                }
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        failureSymbols.Add(symbol);
                        _log.Error(exception);
                        Thread.Sleep(250);
                    }
                }

                var binanceSymbols = new List<string>
                {
                    "MCO",
                    // "IOST"
                };

                foreach (var symbol in binanceSymbols)
                {
                    try
                    {
                        var tradingPair = new TradingPair(symbol, "BTC");
                        lock (_valuationDictionaryLocker)
                        {
                            if (!_valuationDictionary.ContainsKey(tradingPair.BaseSymbol)) { continue; }
                        }

                        var orderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Binance, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.OnlyUseCacheUnlessEmpty);
                        if (orderBook == null || orderBook.Asks == null || !orderBook.Asks.Any() || orderBook.Bids == null || !orderBook.Bids.Any()) { continue; }

                        var priceAtBaseSymbol = (orderBook.BestAsk().Price + orderBook.BestBid().Price) / 2.0m;
                        lock (_valuationDictionaryLocker)
                        {
                            var usdPrice = priceAtBaseSymbol * _valuationDictionary[tradingPair.BaseSymbol];
                            _valuationDictionary[tradingPair.Symbol] = usdPrice;
                        }
                    }
                    catch(Exception exception)
                    {
                        _log.Error(exception);
                        Thread.Sleep(250);
                    }
                }

                var kucoinSymbols = new List<string> { "TIME" };
                foreach (var symbol in kucoinSymbols)
                {
                    try
                    {
                        var tradingPair = new TradingPair(symbol, "BTC");
                        lock (_valuationDictionaryLocker)
                        {
                            if (!_valuationDictionary.ContainsKey(tradingPair.BaseSymbol)) { continue; }
                        }

                        var orderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.KuCoin, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.OnlyUseCacheUnlessEmpty);
                        if (orderBook == null || orderBook.Asks == null || !orderBook.Asks.Any() || orderBook.Bids == null || !orderBook.Bids.Any()) { continue; }

                        var priceAtBaseSymbol = (orderBook.BestAsk().Price + orderBook.BestBid().Price) / 2.0m;
                        lock (_valuationDictionaryLocker)
                        {
                            var usdPrice = priceAtBaseSymbol * _valuationDictionary[tradingPair.BaseSymbol];
                            _valuationDictionary[tradingPair.Symbol] = usdPrice;
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error(exception);
                        Thread.Sleep(250);
                    }
                }

                var cossSymbols = new List<string> { "TIG", "PRSN", "XDCE" };
                foreach (var symbol in cossSymbols)
                {
                    try
                    {
                        var tradingPair = new TradingPair(symbol, "BTC");
                        lock (_valuationDictionaryLocker)
                        {
                            if (!_valuationDictionary.ContainsKey(tradingPair.BaseSymbol)) { continue; }
                        }

                        var orderBook = _exchangeClient.GetOrderBook(ExchangeNameRes.Coss, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.OnlyUseCacheUnlessEmpty);
                        if (orderBook == null || orderBook.Asks == null || !orderBook.Asks.Any() || orderBook.Bids == null || !orderBook.Bids.Any()) { continue; }

                        var priceAtBaseSymbol = (orderBook.BestAsk().Price + orderBook.BestBid().Price) / 2.0m;
                        lock (_valuationDictionaryLocker)
                        {
                            var usdPrice = priceAtBaseSymbol * _valuationDictionary[tradingPair.BaseSymbol];
                            _valuationDictionary[tradingPair.Symbol] = usdPrice;
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error(exception);
                        Thread.Sleep(250);
                    }
                }

                var bitzSymbols = new List<string> { "XRB", "CVT", "LEO", "PPS" };
                var bitzTradingPairs = bitzSymbols.Select(item => new TradingPair(item, "BTC")).ToList();
                bitzTradingPairs.Add(new TradingPair("PPS", "BTC"));
                foreach (var tradingPair in bitzTradingPairs)
                {
                    try
                    {
                        lock (_valuationDictionaryLocker)
                        {
                            if (!_valuationDictionary.ContainsKey(tradingPair.BaseSymbol)) { continue; }
                        }

                        var orderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.Bitz, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.OnlyUseCacheUnlessEmpty);
                        if (orderBook == null || orderBook.Asks == null || !orderBook.Asks.Any() || orderBook.Bids == null || !orderBook.Bids.Any()) { continue; }

                        var priceAtBaseSymbol = (orderBook.BestAsk().Price + orderBook.BestBid().Price) / 2.0m;
                        lock (_valuationDictionaryLocker)
                        {
                            var usdPrice = priceAtBaseSymbol * _valuationDictionary[tradingPair.BaseSymbol];
                            _valuationDictionary[tradingPair.Symbol] = usdPrice;
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error(exception);
                        Thread.Sleep(250);
                    }
                }

                var hitBtcSymbols = new List<string> { "QTUM", "QNTU", "ACT", "CBC", "MLD" };
                foreach (var symbol in hitBtcSymbols)
                {
                    try
                    {
                        var tradingPair = new TradingPair(symbol, "BTC");
                        lock (_valuationDictionaryLocker)
                        {
                            if (!_valuationDictionary.ContainsKey(tradingPair.BaseSymbol)) { continue; }
                        }

                        var orderBook = _exchangeClient.GetOrderBook(IntegrationNameRes.HitBtc, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.OnlyUseCacheUnlessEmpty);
                        if (orderBook == null || orderBook.Asks == null || !orderBook.Asks.Any() || orderBook.Bids == null || !orderBook.Bids.Any()) { continue; }

                        var priceAtBaseSymbol = (orderBook.BestAsk().Price + orderBook.BestBid().Price) / 2.0m;
                        lock (_valuationDictionaryLocker)
                        {
                            var usdPrice = priceAtBaseSymbol * _valuationDictionary[tradingPair.BaseSymbol];
                            _valuationDictionary[tradingPair.Symbol] = usdPrice;
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error(exception);
                        Thread.Sleep(250);
                    }
                }
            }
            finally
            {
                _isWorking = false;
            }
        }

        private List<string> CryptoCompareSymbols => ResUtil.Get<List<string>>("cryptocompare-symbols.json", typeof(TradeResDummy).Assembly);
    }
}
