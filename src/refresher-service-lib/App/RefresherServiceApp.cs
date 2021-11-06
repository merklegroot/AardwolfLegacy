using cache_lib.Models;
using console_lib;
using cryptocompare_client_lib;
using exchange_client_lib;
using log_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using trade_res;
using workflow_client_lib;

namespace refresher_service_lib.App
{
    public class RefresherServiceApp : IRefresherServiceApp
    {
        private readonly IExchangeClient _exchangeClient;
        private readonly ICryptoCompareClient _cryptoCompareClient;
        private readonly IWorkflowClient _workflowClient;
        private readonly ILogRepo _log;

        public RefresherServiceApp(
            IExchangeClient exchangeClient,
            ICryptoCompareClient cryptoCompareClient,
            IWorkflowClient workflowClient,
            ILogRepo log)
        {
            _exchangeClient = exchangeClient;
            _workflowClient = workflowClient;
            _cryptoCompareClient = cryptoCompareClient;
            _log = log;
        }

        public void Run()
        {
            Iterate();
        }

        private void Iterate()
        {
            Continuously(() =>
            {
                RefreshValuations();
            });

            while (true)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
            }
        }

        private void IterateOld()
        {
            Continuously(() => RefreshValuations());

            Continuously(() => RefreshTradingPairs(CachePolicy.OnlyUseCacheUnlessEmpty));
            Continuously(() => RefreshTradingPairs(CachePolicy.AllowCache));

            Continuously(() => RefreshCossOrderBooks());
            Continuously(() => RefreshBinanceOrderBooks());
            Continuously(() => RefreshKucoinOrderBooks());
            Continuously(() => RefreshQryptosOrderBooks());

            while (true)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(50));
            }
        }

        private Task Continuously(Action method)
        {
            var task = new Task(() =>
            {
                while (true)
                {
                    try
                    {
                        method();
                    }
                    catch (Exception exception)
                    {
                        try
                        {
                            _log.Error(exception);
                        }
                        catch
                        {
                        }
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(2.5));
                }
            }, TaskCreationOptions.LongRunning);

            task.Start();

            return task;
        }

        private void RefreshValuations()
        {
            var symbols = new List<string>
            {
                "ETH", "BTC", "NEO", "LTC", "CHX"
            };

            foreach (var symbol in symbols)
            {
                try
                {
                    Console.WriteLine($"Getting {symbol} value...");
                    var result = _workflowClient.GetUsdValueV2(symbol, CachePolicy.AllowCache);
                    Console.WriteLine($"  {symbol}: {result.UsdValue}");
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }

                Thread.Sleep(TimeSpan.FromSeconds(25));
            }

            Thread.Sleep(TimeSpan.FromSeconds(5));
        }

        private void RefreshTradingPairs(CachePolicy cachePolicy)
        {
            var exchanges = _exchangeClient.GetExchanges();
            var tasks = new List<Task>();
            foreach (var exchange in exchanges)
            {
                var task = new Task(() =>
                {
                    try
                    {
                        ConsoleWrapper.WriteLine($"  {exchange.Name} - Starting get trading pairs, {cachePolicy}.");
                        _exchangeClient.GetTradingPairs(exchange.Name, cachePolicy);
                        ConsoleWrapper.WriteLine($"  {exchange.Name} - Done with get trading pairs, {cachePolicy}.");
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"Failed to get trading pairs for exchange {exchange?.Name} with cache policy {cachePolicy}.");
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
        }

        private class ExchangeTradingPair
        {           
            public ExchangeTradingPair(string exchange, string symbol, string baseSymbol)
            {
                Exchange = exchange;
                Symbol = symbol;
                BaseSymbol = baseSymbol;
            }

            public ExchangeTradingPair() { }

            public string Exchange { get; set; }
            public string Symbol { get; set; }
            public string BaseSymbol { get; set; }
        }

        private void RefreshBinanceOrderBooks()
        {
            const string Exchange = ExchangeNameRes.Binance;

            var commoditiesWithBoth = new List<string>
            {
                "SNM", "OMG", "LINK", "ENJ",
                "VET", "SUB", "DENT", "WTC",
                "ZIL", "BLZ"
            };

            var exchangeTradingPairs = new List<ExchangeTradingPair>();
            foreach (var symbol in commoditiesWithBoth)
            {
                foreach (var baseSymbol in new List<string> { "ETH", "BTC" })
                {
                    exchangeTradingPairs.Add(new ExchangeTradingPair(Exchange, symbol, baseSymbol));
                }
            }

            var pairs = new Dictionary<string, string>
            {
                { "ETH", "BTC" },
                { "BCH", "BTC" },
                { "LTC", "BTC" },
                { "NEO", "BTC" },
                { "ELF", "ETH" },
            };

            pairs.Keys.ToList().ForEach(symbol =>
            {
                var baseSymbol = pairs[symbol];
                exchangeTradingPairs.Add(new ExchangeTradingPair(Exchange, symbol, baseSymbol));
            });


            RefreshOrderBooks(exchangeTradingPairs);
        }

        private void RefreshCossOrderBooks()
        {
            const string Exchange = ExchangeNameRes.Coss;

            var commoditiesWithBoth = new List<string>
            {
                "LA", "LALA", "SNM",
                "OMG", "LINK", "SUB", "ENJ", "BCH",
                "DAT", "WTC", "LSK", "KNC",
                "POE",
                "CS",
                "GAT",
                "PRL",
                "VZT",
                "IND",
                "CVC",
                "CAN",
                "BLZ"
            }.Distinct().ToList();

            var pairs = new List<(string, string)>
            {
                ("ETH", "BTC"),
                ("LTC", "BTC"),
                ("LTC", "COSS")
            };

            RefreshExchangeOrderBooks(Exchange, commoditiesWithBoth, pairs);
        }

        private void RefreshKucoinOrderBooks()
        {
            const string Exchange = ExchangeNameRes.KuCoin;
            var commoditiesWithBoth = new List<string>
            {
                "LA", "LALA", "SNM", "VET",
                "CS", "COV", "PRL", "ZIL", "MTN",
                "GAT", "LTC", "DENT", "ARN",
                "SUB",
                "PRL",
                "WTC",
                "OMG",
                "ZIL",
                "DAT"
            };

            var pairs = new List<(string, string)>
            {
                ( "ETH", "BTC" ),
                ( "CS", "USDT" ),
                ( "DENT", "NEO" ),
                ( "NEO", "BTC" ),
                ( "MTH", "BTC" ),
                ( "ELF", "ETH" ),
                ( "BCH", "ETH" ),
                ( "BTC", "USDT" ),
                ( "DASH", "BTC" ),
            };

            RefreshExchangeOrderBooks(Exchange, commoditiesWithBoth, pairs);
        }

        private void RefreshQryptosOrderBooks()
        {
            const string Exchange = ExchangeNameRes.Qryptos;
            var commoditiesWithBoth = new List<string>
            {
                "VZT", "STU", "IND", "DENT"
            };

            var pairs = new List<(string, string)>
            {
                ("ETH", "BTC"),
                ("DENT", "QASH"),
                ("BCH", "BTC")
            };

            RefreshExchangeOrderBooks(Exchange, commoditiesWithBoth, pairs);
        }

        private void RefreshExchangeOrderBooks(
            string exchange,
            List<string> commoditiesWithBoth,
            List<(string Symbol, string BaseSymbol)> pairs)
        {
            var exchangeTradingPairs = new List<ExchangeTradingPair>();
            if (commoditiesWithBoth != null)
            {
                foreach (var symbol in commoditiesWithBoth)
                {
                    foreach (var baseSymbol in new List<string> { "ETH", "BTC" })
                    {
                        exchangeTradingPairs.Add(new ExchangeTradingPair(exchange, symbol, baseSymbol));
                    }
                }
            }

            if (pairs != null)
            {
                pairs.ForEach(pair =>
                {
                    exchangeTradingPairs.Add(new ExchangeTradingPair(exchange, pair.Symbol, pair.BaseSymbol));
                });
            }

            RefreshOrderBooks(exchangeTradingPairs);
        }

        private void RefreshOrderBooks(List<ExchangeTradingPair> exchangeTradingPairs)
        {
            for (var i = 0; i < exchangeTradingPairs.Count; i++)
            {
                if (i != 0)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                }

                var exchangeTradingPair = exchangeTradingPairs[i];

                const CachePolicy Policy = CachePolicy.AllowCache;

                ConsoleWrapper.WriteLine($"Getting order book for {exchangeTradingPair.Exchange} {exchangeTradingPair.Symbol}-{exchangeTradingPair.BaseSymbol}");
                try
                {
                    var orderBook = _exchangeClient.GetOrderBook(
                        exchangeTradingPair.Exchange,
                        exchangeTradingPair.Symbol,
                        exchangeTradingPair.BaseSymbol,
                        Policy);
                }
                catch (Exception exception)
                {
                    _log.Error($"Failed to get order book for Exchange: {exchangeTradingPair?.Exchange}, Symbol: {exchangeTradingPair?.Symbol}, {exchangeTradingPair.BaseSymbol}, CachePolicy: {Policy}.");

                    ConsoleWrapper.WriteLine(exception);
                    try { _log.Error(exception); } catch { }
                }
            }
        }
    }
}
