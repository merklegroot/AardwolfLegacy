using binance_lib;
using cache_lib.Models;
using coss_lib;
using hitbtc_lib;
using idex_integration_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using trade_lib;
using trade_model;

namespace idex_con
{
    public class App
    {
        private readonly IIdexIntegration _idex;
        private readonly IHitBtcIntegration _hitBtc;
        private readonly IBinanceIntegration _binance;
        private readonly ICossIntegration _coss;

        public App(IIdexIntegration idex, IHitBtcIntegration hitBtc, IBinanceIntegration binance, ICossIntegration coss)
        {
            _idex = idex;
            _hitBtc = hitBtc;
            _binance = binance;
            _coss = coss;
        }

        private bool _keepRunning = true;

        public void Run()
        {
            while (_keepRunning)
            {
                try
                {
                    Iterate();
                    var sleepTime = TimeSpan.FromSeconds(10);
                    Console.WriteLine($"Iteration complete. Sleeping for {sleepTime.TotalSeconds} seconds...");
                    Thread.Sleep(sleepTime);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
        }

        public void Iterate()
        {
            Console.WriteLine("Fetching trading pairs from Apis...");

            var otherIntegrations = new List<ITradeIntegration> { _hitBtc, _binance, _coss };

            var idexTradingPairsTask = Task.Run(() => _idex.GetTradingPairs(CachePolicy.AllowCache));
            var idexTradingPairs = idexTradingPairsTask.Result;

            var otherIntegrationTradingPairTasks = otherIntegrations.Select(item =>
            new
            {
                Integration = item,
                TradingPairTask = Task.Run(() =>
                {
                    try
                    {
                        return item.GetTradingPairs(CachePolicy.AllowCache);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine($"Failed to retrieve {item.Name} tradingPairs");
                        Console.WriteLine(exception);
                        return new List<TradingPair>();
                    }
                })
            });

            Console.WriteLine($"Idex has {idexTradingPairs.Count} total trading pairs.");

            var integrationTradingPairs = new List<TradingPair>();
            foreach (var task in otherIntegrationTradingPairTasks)
            {
                Console.WriteLine($"{task.Integration.Name} has {task.TradingPairTask.Result.Count} total trading pairs.");
                integrationTradingPairs.AddRange(task.TradingPairTask.Result);
            }

            var tradingPairsOfInterest = idexTradingPairs
                .Where(idexTradingPair => integrationTradingPairs.Any(tp => tp.Equals(idexTradingPair))
                ).ToList();

            Console.WriteLine($"There are {tradingPairsOfInterest.Count} intersecting trading pairs.");

            KeepTradingPairsCurrent(tradingPairsOfInterest);
        }

        private void KeepTradingPairsCurrent(List<TradingPair> tradingPairs)
        {
            for (var index = 0; index < tradingPairs.Count; index++)
            {
                var tradingPair = tradingPairs[index];
                Console.WriteLine($"{index + 1} of {tradingPairs.Count}");
                try
                {
                    RefreshTradingPair(tradingPair);
                    Thread.Sleep(TimeSpan.FromSeconds(0.25));
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }
            }
        }

        private void RefreshTradingPair(TradingPair tradingPair)
        {
            Console.WriteLine($"Refresing {tradingPair}");
            var orderBook = _idex.GetOrderBook(tradingPair);
            Console.WriteLine($"  {tradingPair} has {orderBook?.Asks?.Count ?? 0} asks and {orderBook?.Bids?.Count ?? 0} bids.");
            Console.WriteLine("---");
        }
    }
}
