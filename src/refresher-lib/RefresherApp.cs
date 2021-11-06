using console_lib;
using cryptocompare_lib;
using iridium_lib;
using log_lib;
using refresher_lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using trade_contracts;
using cache_lib.Models;
using trade_model;
using exchange_client_lib;

namespace refesher_lib
{
    public class RefresherApp : IRefresherApp
    {
        private static TimeSpan TimeBetweenIterations = TimeSpan.FromSeconds(10);
        private static TimeSpan TimeBetweenReading = TimeSpan.FromSeconds(5);

        private readonly IExchangeClient _exchangeClient;
        private readonly ICryptoCompareIntegration _cryptoCompareIntegration;
        private readonly ILogRepo _log;

        public RefresherApp(
            ICryptoCompareIntegration cryptoCompareIntegration,
            IExchangeClient exchangeClient,
            ILogRepo log)
        {
            _cryptoCompareIntegration = cryptoCompareIntegration;
            _exchangeClient = exchangeClient;

            _log = log;
        }

        public void Run()
        {
            while (true)
            {
                try
                {
                    Iterate();
                }
                catch(Exception exception)
                {
                    _log.Error(exception);
                }

                ConsoleWrapper.WriteLine("Sleeping for a bit.");
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        private void Iterate()
        {
            ConsoleWrapper.WriteLine("Starting iteration.");

            var exchanges = _exchangeClient.GetExchanges();
            var tasks = new List<Task>();
            foreach (var exchange in exchanges)
            {
                var task = new Task(() =>
                {
                    try
                    {
                        ConsoleWrapper.WriteLine($"  {exchange.Name} - Starting get trading pairs, only use cache unless empty.");
                        _exchangeClient.GetTradingPairs(exchange.Name, CachePolicy.OnlyUseCacheUnlessEmpty);
                        ConsoleWrapper.WriteLine($"  {exchange.Name} - Done with get trading pairs, only use cache unless empty.");

                        ConsoleWrapper.WriteLine($"  {exchange.Name} - Starting get trading pairs, allow cache.");
                        _exchangeClient.GetTradingPairs(exchange.Name, CachePolicy.AllowCache);
                        ConsoleWrapper.WriteLine($"  {exchange.Name} - Done with get trading pairs, allow cache.");
                    }
                    catch(Exception exception)
                    {
                        _log.Error(exception);
                    }
                }, TaskCreationOptions.LongRunning);

                task.Start();

                tasks.Add(task);
            }

            foreach (var task in tasks)
            {
                task.Wait();
            }

            ConsoleWrapper.WriteLine("Iteration complete.");
        }

        public void RunOld()            
        {
            var cryptoCompareTask = new Task(() => KeepCryptoCompareFresh(_cryptoCompareIntegration), TaskCreationOptions.LongRunning);
            cryptoCompareTask.Start();

            var exchanges = _exchangeClient.GetExchanges();

            KeepTradeIntegrationsFresh(exchanges);

            cryptoCompareTask.Wait();
        }

        private void KeepTradeIntegrationsFresh(List<Exchange> exchanges)
        {
            var refresherTasks = new List<Task>();
            foreach (var exchange in exchanges)
            {
                var task = new Task(() => KeepIntegrationFresh(exchange, exchanges), TaskCreationOptions.LongRunning);
                task.Start();
                refresherTasks.Add(task);
            }

            refresherTasks.ForEach(task => task.Wait());
        }

        private static object GetIntersectionsLocker = new object();
        private List<TradingPair> GetIntersections(Exchange targetExchange, List<Exchange> allIntegrations)
        {
            lock (GetIntersectionsLocker)
            {
                return GetIntersectionsUnwrapped(targetExchange, allIntegrations);
            }
        }

        private List<TradingPair> GetIntersectionsUnwrapped(Exchange exchange, List<Exchange> allIntegrations)
        {
            ConsoleWrapper.WriteLine($"Getting intersections for {exchange}.");
            var tradingPairDictionary = new Dictionary<string, List<TradingPair>>(StringComparer.InvariantCultureIgnoreCase);
            var tradingPairTasks = new List<Task>();
            foreach (var compIntegration in allIntegrations)
            {
                if (string.IsNullOrWhiteSpace(compIntegration.Name))
                {
                    throw new ApplicationException("Integration name must not be null.");
                }

                tradingPairTasks.Add(Task.Run(() =>
                {
                    List<TradingPair> compTradingPairs;
                    try
                    {
                        ConsoleWrapper.WriteLine($"  Getting trading pairs for {compIntegration}.");
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        compTradingPairs = _exchangeClient.GetTradingPairs(compIntegration.Name, CachePolicy.ForceRefresh);
                        stopwatch.Stop();
                        ConsoleWrapper.WriteLine($"  Done getting trading pairs for {compIntegration}. It took {stopwatch.ElapsedMilliseconds.ToString("N4")} ms");
                    }
                    catch (Exception exception)
                    {
                        ConsoleWrapper.WriteLine($"  Failed to get trading pairs for {compIntegration}.");
                        compTradingPairs = new List<TradingPair>();
                        _log.Error(exception);                        
                    }

                    tradingPairDictionary[compIntegration.Name] = compTradingPairs;
                }));
            }
            tradingPairTasks.ForEach(task => task.Wait());

            var intersections = new List<TradingPair>();
            var tradingPairs = tradingPairDictionary[exchange.Name];
            foreach (var tradingPair in tradingPairs)
            {
                foreach (var key in tradingPairDictionary.Keys.Where(queryKey => !string.Equals(queryKey, exchange.Name, StringComparison.Ordinal)))
                {
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        _log.Error($"{nameof(key)} must not be null.");
                        continue;
                    }

                    var compTradingPairs = tradingPairDictionary[key];
                    if (compTradingPairs.Any(queryCompTp => queryCompTp.Equals(tradingPair)))
                    {
                        intersections.Add(tradingPair);
                        break;
                    }
                }
            }

            ConsoleWrapper.WriteLine($"Done intersections for {exchange}.");

            return intersections;
        }

        private void KeepIntegrationFresh(Exchange targetExchange, List<Exchange> comps)
        {
            while (true)
            {
                var intersections = GetIntersections(targetExchange, comps);

                foreach (var tradingPair in intersections)
                {
                    try
                    {
                        RefreshPair(tradingPair, targetExchange);
                    }
                    catch (Exception exception)
                    {
                        _log.Error(exception);
                    }
                }
            }
        }

        private void KeepCryptoCompareFresh(ICryptoCompareIntegration cryptoCompareIntegration)
        {
            while (true)
            {
                CryptoCompareIteration(cryptoCompareIntegration);
                Thread.Sleep(TimeSpan.FromSeconds(30));
            }
        }

        private void CryptoCompareIteration(ICryptoCompareIntegration cryptoCompareIntegration)
        {
            var symbolsToCheck = _exchangeClient.GetCryptoCompareSymbols();            

            foreach (var symbol in symbolsToCheck)
            {
                ConsoleWrapper.WriteLine($"CryptoCompare -- Refreshing {symbol}");
                try
                {
                    cryptoCompareIntegration.GetPrices(symbol, CachePolicy.AllowCache);
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                }

                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        //private void RunOrdered(List<ExchangeContract> integrations)
        //{
        //    while (true)
        //    {
        //        try
        //        {
        //            Iterate(integrations);
        //        }
        //        catch (Exception exception)
        //        {
        //            ConsoleWrapper.WriteLine("Iteration failed.");
        //            ConsoleWrapper.WriteLine(exception);
        //            _log.Error(exception);
        //        }

        //        ConsoleWrapper.WriteLine("Iteration complete.");
        //        ConsoleWrapper.WriteLine($"Sleeping for {TimeBetweenIterations.TotalSeconds} seconds");

        //        Thread.Sleep(TimeBetweenIterations);
        //    }
        //}

        private void Iterate(List<Exchange> exchanges)
        { 
            var tasks = exchanges.Select(exchange => Task.Run(() =>
            {
                try
                {
                    return new IntegrationEx
                    {
                        Exchange = exchange,
                        TradingPairs = _exchangeClient.GetTradingPairs(exchange.Name, CachePolicy.ForceRefresh)
                    };
                }
                catch(Exception exception)
                {
                    _log.Error(exception);
                    return null;
                }
            }));

            
            var extendedIntegrations = tasks
                .Where(item => item.Result != null)
                .Select(item => item.Result)
                .ToList();

            for (var i = 0; i < extendedIntegrations.Count; i++)
            {
                var a = extendedIntegrations[i];
                var intersections = new List<TradingPair>();
                for (var j = 0; j < extendedIntegrations.Count; j++)
                {
                    if (i == j) { continue; }
                    
                    var b = extendedIntegrations[j];
                    var matches =
                        a.TradingPairs.Where(tpA => b.TradingPairs.Any(tpB => tpA.Equals(tpB)))
                        .ToList();

                    intersections.AddRange(matches);
                }

                a.Intersections = intersections.Distinct().ToList();
            }

            var refreshTasks = new List<Task>();
            for (var i = 0; i < extendedIntegrations.Count
                // && i < 1
                ; i++)
            {
                var ei = extendedIntegrations[i];
                var task = Task.Run(() =>
                {
                    RefreshPairs(ei.Intersections, ei.Exchange);
                    ConsoleWrapper.WriteLine($"{ei.Exchange}");
                });
                refreshTasks.Add(task);
            }

            foreach (var task in refreshTasks)
            {
                task.Wait();
            }
        }

        private void RefreshPairs(List<TradingPair> intersections, Exchange exchange)
        {
            ConsoleWrapper.WriteLine($"[{exchange}] -- Starting iteration...");
            ConsoleWrapper.WriteLine($"[{exchange}] -- There are {intersections.Count} intersections.");
            for (var i = 0; i < intersections.Count; i++)
            {
                var tradingPair = intersections[i];
                ConsoleWrapper.WriteLine($"[{exchange}] -- {i+1} of {intersections.Count}");
                RefreshPair(tradingPair, exchange);
            }
            ConsoleWrapper.WriteLine($"[{exchange}] -- Iteration complete.");
        }        

        private void RefreshPair(TradingPair tradingPair, Exchange exchange)
        {
            bool shouldSleep = true;

            ConsoleWrapper.WriteLine($"[{exchange}] -- Refreshing {tradingPair}");
            try
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                if (exchange.IsRefreshable)
                {
                    var result = _exchangeClient.RefreshOrderBook(exchange.Name, tradingPair.Symbol, tradingPair.BaseSymbol);
                    shouldSleep = result.WasRefreshed;
                    if (result.CacheAge.HasValue)
                    {
                        ConsoleWrapper.WriteLine($"[{exchange}] -- The cache was {result.CacheAge.Value.TotalMinutes} minutes old.");
                    }

                    if (result.WasRefreshed)
                    {
                        ConsoleWrapper.WriteLine($"[{exchange}] -- Pair WAS refreshed.");
                    }
                    else
                    {
                        ConsoleWrapper.WriteLine($"[{exchange}] -- Pair was NOT refreshed.");
                    }
                }
                else
                {
                    var orderBook = _exchangeClient.GetOrderBook(exchange.Name, tradingPair.Symbol, tradingPair.BaseSymbol, CachePolicy.ForceRefresh);
                    ConsoleWrapper.WriteLine($"[{exchange}] -- {orderBook?.Asks?.Count ?? 0} Asks and {orderBook?.Bids?.Count ?? 0} Bids.");
                }
                stopWatch.Stop();
                ConsoleWrapper.WriteLine($"[{exchange}] -- It took {stopWatch.ElapsedMilliseconds} ms.");                
            }
            catch (Exception exception)
            {
                shouldSleep = true;

                ConsoleWrapper.WriteLine($"[{exchange}] -- Failed to get order book.");
                ConsoleWrapper.WriteLine($"[{exchange}] -- {exception.Message}");
            }

            if (shouldSleep)
            {
                ConsoleWrapper.WriteLine($"Sleeping for {TimeBetweenReading.TotalSeconds} seconds");
                Thread.Sleep(TimeBetweenReading);
            }

            ConsoleWrapper.WriteLine("");
        }

        private class IntegrationEx
        {
            public Exchange Exchange { get; set; }
            public List<TradingPair> TradingPairs { get; set; }
            public List<TradingPair> Intersections { get; set; }
        }
    }
}
