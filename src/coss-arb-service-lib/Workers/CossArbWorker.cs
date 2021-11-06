using arb_workflow_lib;
using cache_lib.Models;
using config_client_lib;
using coss_arb_lib;
using coss_arb_service_lib.res;
using exchange_client_lib;
using linq_lib;
using log_lib;
using res_util_lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using task_lib;
using trade_constants;
using trade_model;

namespace coss_arb_service_lib.Workflows
{
    public interface ICossArbWorker
    {
        void Run();
    }

    public class CossArbWorker : ICossArbWorker
    {
        private readonly IConfigClient _configClient;
        private readonly IExchangeClient _exchangeClient;
        private readonly ICossArbUtil _cossArbUtil;
        private readonly IArbWorkflowUtil _arbWorkflowUtil;
        private readonly ILogRepo _log;

        public CossArbWorker(
            IConfigClient configClient,
            IExchangeClient exchangeClient,
            ICossArbUtil cossArbUtil,
            IArbWorkflowUtil arbWorkflowUtil,
            ILogRepo log)
        {
            _configClient = configClient;
            _exchangeClient = exchangeClient;
            _cossArbUtil = cossArbUtil;
            _arbWorkflowUtil = arbWorkflowUtil;
            _log = log;

            _log.EnableConsole();
        }

        private List<TradingPair> _tradingPairs;
        private int _refreshIndex = 0;

        public void Run()
        {
            _tradingPairs = _exchangeClient.GetTradingPairs(IntegrationNameRes.Coss, CachePolicy.AllowCache);

            var jobs = new List<Action>
            {
                // PerformTimeSync,
                // AcquireCossProcessor,
                // AutoEthProcess,
                AutoSymbolProcessV2,
                // AutoStableProcessor,


                // GroupProcessor
                // () => _cossArbUtil.AutoEthBtcV2()
            };

            var runAllJobs = new Action(() =>
            {
                foreach (var job in jobs)
                {
                    try
                    {
                        PerformTimeSync();
                        job();
                    }
                    catch (Exception exception)
                    {
                        _log.Error(exception);
                    }

                    RefreshNextTradingPair(5);
                }

                var timeToWait = TimeSpan.FromMinutes(1.5d);
                _log.Info($"Coss arb iteration complete. Sleeping for {timeToWait}.");
                Thread.Sleep(timeToWait);
            });

            var allJobsTask = LongRunningTask.Run(() =>
            {
                Forever(runAllJobs);
            });

            //var tasks = new List<Task>();
            //foreach (var job in jobs)
            //{
            //    tasks.Add(LongRunningTask.Run(() => Forever(job)));
            //}

            //tasks.ForEach(task => task.Wait());
        }

        private void RefreshNextTradingPair(int times = 1)
        {
            for (int i = 0; i < times; i++)
            {
                var pair = _tradingPairs[_refreshIndex];
                try
                {
                    _exchangeClient.GetUserTradeHistoryForTradingPair(IntegrationNameRes.Coss, pair.Symbol, pair.BaseSymbol, CachePolicy.AllowCache);
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                }
                finally
                {
                    _refreshIndex = (_refreshIndex + 1) % _tradingPairs.Count;
                }
            }
        }

        private void GroupProcessor()
        {
            var subProcesses = new List<Action>
            {
                // AcquireLtcProcessor,
                AutoTusdProcess,
                AutoBwtProcess,
                AutoStableProcessor,
                AutoXdceProcessor
            };

            foreach(var subProcess in subProcesses)
            {
                try { subProcess(); } catch (Exception exception) { _log.Error(exception); }
            }
        }       

        private void AutoSellProcess()
        {
            _log.Info("CossArbWorker -- Beginning new AutoSell iteration.");
            try
            {
                var cossAgentConfig = _configClient.GetCossAgentConfig();
                if (!(cossAgentConfig?.IsCossAutoTradingEnabled ?? false))
                {
                    _log.Info("CossArbWorker -- Coss Auto Trading is disabled. Sleeping...");
                }
                else
                {
                    _cossArbUtil.AutoSell();
                }
            }
            catch (Exception exception)
            {
                _log.Info("CossArbWorker -- AutoSell encountered an exception.");
                _log.Error(exception);
            }
            finally
            {
                _log.Info("CossArbWorker -- AutoSell iteration complete. Sleeping...");
                Thread.Sleep(TimeSpan.FromSeconds(30));
            }
        }

        private void AutoOpenBidProcess()
        {
            _log.Info("CossArbWorker -- Beginning new AutoOpenBid iteration.");

            try
            {
                var cossAgentConfig = _configClient.GetCossAgentConfig();
                if (!(cossAgentConfig?.IsCossAutoTradingEnabled ?? false))
                {
                    _log.Info("CossArbWorker -- Coss Auto Trading is disabled. Sleeping...");
                }
                else
                {
                    _cossArbUtil.OpenBid();
                }
            }
            finally
            {
                _log.Info("CossArbWorker -- AutoOpenBid iteration complete. Sleeping...");
                Thread.Sleep(TimeSpan.FromSeconds(30));
            }
        }

        private void AutoEthProcess()
        {
            _log.Info("CossArbWorker -- Beginning new AutoEthBtc iteration.");

            try
            {
                if (!IsCossAutoTradingEnabled())
                {
                    _log.Info("CossArbWorker.AutoEthBtcProcess() -- Coss Auto Trading is disabled. Sleeping...");
                }
                else if (IsBinanceIsOnMaintenance())
                {
                    _log.Info("CossArbWorker.AutoEthBtcProcess() -- Binance is on maintenance. Sleeping...");
                }
                else
                {
                    var processesAndDescs = new List<(Action action, string desc)>
                    {
                        (() => _cossArbUtil.AutoEthBtcV2(), "AutoEthBtc"),
                        (() => _cossArbUtil.AutoEthUsdt(), "AutoEthUsdt"),
                        (() => _cossArbUtil.AutoTusdWithReverseBinanceSymbol("ETH"), "Auto ETH-TUSD"),
                    };

                    foreach (var processAndDesc in processesAndDescs)
                    {
                        var process = processAndDesc.action;
                        var desc = processAndDesc.desc;

                        _log.Info($"Beginning \"{desc}\"");
                        try
                        {                            
                            process();
                        }
                        catch (Exception exception)
                        {
                            _log.Error($"\"{desc}\" failed.{Environment.NewLine}{exception.Message}");
                            _log.Error(exception);
                        }
                        finally
                        {
                            _log.Info($"Completed \"{desc}\".");
                        }
                    }
                }
            }
            finally
            {
                _log.Info("CossArbWorker -- AutoEthBtc iteration complete. Sleeping...");
                Thread.Sleep(TimeSpan.FromMinutes(2.5));
            }
        }

        private void AutoStableProcessor()
        {
            _log.Info("CossArbWorker -- Beginning new AutoEthBtc iteration.");

            try
            {
                if (!IsCossAutoTradingEnabled())
                {
                    _log.Info("CossArbWorker.AutoStableProcessor() -- Coss Auto Trading is disabled. Sleeping...");
                }
                else if (IsBinanceIsOnMaintenance())
                {
                    _log.Info("CossArbWorker.AutoStableProcessor() -- Binance is on maintenance. Sleeping...");
                }
                else
                {
                    var processesAndDescs = new List<(Action action, string desc)>
                    {
                        (() =>_cossArbUtil.AutoBtcGusd(), "AutoBtcGusd"),
                        (() =>_cossArbUtil.AutoBtcUsdc(), "AutoBtcUsdc"),
                        // (() =>_cossArbUtil.AutoBtcUsdt(), "AutoBtcUsdt"),
                    };

                    foreach (var processAndDesc in processesAndDescs)
                    {
                        var process = processAndDesc.action;
                        var desc = processAndDesc.desc;

                        _log.Info($"Beginning \"{desc}\"");
                        try
                        {
                            process();
                        }
                        catch (Exception exception)
                        {
                            _log.Error($"\"{desc}\" failed.{Environment.NewLine}{exception.Message}");
                            _log.Error(exception);
                        }
                        finally
                        {
                            _log.Info($"Completed \"{desc}\".");
                        }
                    }
                }
            }
            finally
            {
                _log.Info("CossArbWorker -- AutoStableProcessor iteration complete. Sleeping...");
                Thread.Sleep(TimeSpan.FromMinutes(2.5));
            }
        }

        private void AutoSymbolProcess()
        {
            _log.Info("CossArbWorker -- Beginning new AutoSymbol iteration.");

            try
            {
                if (!IsCossAutoTradingEnabled())
                {
                    _log.Info("CossArbWorker.AutoSymbolProcess() -- Coss Auto Trading is disabled. Sleeping...");
                }
                else if (IsBinanceIsOnMaintenance())
                {
                    _log.Info("CossArbWorker.AutoSymbolProcess() -- Binance is on maintenance. Sleeping...");
                }
                else
                {
                    // CossArbServiceLibResDummy
                    var symbolsAndComps = ResUtil.Get<Dictionary<string, string>>("comps.json", typeof(CossArbServiceLibResDummy).Assembly);
                    var symbols = symbolsAndComps.Keys.ToList().Shuffle();
                    foreach (var symbol in symbols)
                    {
                        var comp = symbolsAndComps[symbol];

                        try
                        {
                            // Console.WriteLine($"Beginning coss auto symbol for {symbol}.");
                            _cossArbUtil.AutoSymbol(symbol, comp);
                            // Console.WriteLine($"Compeleted coss auto symbol for {symbol}.");

                            
                        }
                        catch (Exception exception)
                        {
                            _log.Error($"Auto Symbol failed for {symbol}, {comp}.");
                            _log.Error(exception);
                        }

                        try
                        {
                            RefreshNextTradingPair();
                        }
                        catch(Exception exception)
                        {
                            _log.Error(exception);
                        }
                    }
                }
            }
            finally
            {
                _log.Info("CossArbWorker -- AutoSymbol iteration complete. Sleeping...");
                Thread.Sleep(TimeSpan.FromMinutes(2.5));
            }
        }

        public void AutoSymbolProcessV2()
        {
            var effectiveSymbolsAndComps = ResUtil.Get<Dictionary<string, string>>("v2comps.json", typeof(CossArbServiceLibResDummy).Assembly);
            AutoSymbolProcessV2Dictionary(null, false);
        }

        public void AutoSymbolProcessV2Symbol(string symbol, string compExchange, bool skipWait)
        {
            AutoSymbolProcessV2Dictionary(new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                { symbol, compExchange }
            }, skipWait);
        }

        public void AutoSymbolProcessV2Dictionary(Dictionary<string, string> symbolsAndComps, bool skipWait)
        {
            const decimal MaxUsdValueToOwnForEachSymbol = 50;

            _log.Info("CossArbWorker -- Beginning new AutoSymbol iteration.");

            try
            {
                var isBinanceOnMaintenance = IsBinanceIsOnMaintenance();

                var approvedBaseSymbols = new List<string>
                {
                    "ETH", "BTC", "COSS", "TUSD"
                };

                var arbTradingPairs = _exchangeClient.GetTradingPairs(IntegrationNameRes.Coss, CachePolicy.AllowCache)
                    .Where(queryTradingPair => approvedBaseSymbols.Any(queryApprovedBaseSymbol => string.Equals(queryTradingPair.BaseSymbol, queryApprovedBaseSymbol, StringComparison.InvariantCultureIgnoreCase)))
                    .ToList();

                var cossAgentConfig = _configClient.GetCossAgentConfig();
                if (!(cossAgentConfig?.IsCossAutoTradingEnabled ?? false))
                {
                    _log.Info("CossArbWorker -- Coss Auto Trading is disabled. Sleeping...");
                    return;
                }

                var effectiveSymbolsAndComps = symbolsAndComps ?? ResUtil.Get<Dictionary<string, string>>("v2comps.json", typeof(CossArbServiceLibResDummy).Assembly);
                var symbols = effectiveSymbolsAndComps.Keys.ToList().Shuffle();
                for (var symbolIndex = 0; symbolIndex < symbols.Count; symbolIndex++)
                {
                    // for now, let's wait a few seconds between each pair to lower the number of times that we're hitting the rate limit.
                    /// TODO: Remove this after the getting rid of the unnecessary cancel -> re-orders.
                    if (symbolIndex != 0)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(5));
                    }

                    var symbol = symbols[symbolIndex];
                    _log.Info($"Beginning Coss Auto-Symbol process for {symbol}. ({symbolIndex + 1}/{symbols.Count})");

                    try
                    {
                        var comp = effectiveSymbolsAndComps[symbol];
                        if (isBinanceOnMaintenance && string.Equals(comp, IntegrationNameRes.Binance, StringComparison.InvariantCultureIgnoreCase))
                        {
                            _log.Info($"Skipping coss's {symbol}-binance comp since binance is on maintenance.");
                            continue;
                        }

                        if (string.Equals(comp, IntegrationNameRes.Binance, StringComparison.InvariantCultureIgnoreCase))
                        {
                            _cossArbUtil.AcquireAgainstBinanceSymbolV5(symbol);
                        }
                        else if (string.Equals(comp, IntegrationNameRes.Idex, StringComparison.InvariantCultureIgnoreCase)
                            || string.Equals(comp, IntegrationNameRes.Qryptos, StringComparison.InvariantCultureIgnoreCase))
                        {
                            _arbWorkflowUtil.AutoSymbol(symbol, IntegrationNameRes.Coss, comp, null, false, true, MaxUsdValueToOwnForEachSymbol);
                        }
                        else
                        {
                            _arbWorkflowUtil.AutoSymbol(symbol, IntegrationNameRes.Coss, comp, null, false, false, MaxUsdValueToOwnForEachSymbol);
                        }
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"CossArbWorker.AutoSymbolProcessV2Dictionary() failed for symbol \"{symbol}\".{Environment.NewLine}{exception.Message}");
                        _log.Error(exception);

                        var timeToSleepFromException = TimeSpan.FromSeconds(5);
                        Thread.Sleep(timeToSleepFromException);
                    }
                }
            }
            finally
            {
                _log.Info("CossArbWorker -- AutoSymbol iteration complete. Sleeping...");
                if (!skipWait)
                {
                    Thread.Sleep(TimeSpan.FromMinutes(2.5));
                }
            }
        }

        private void AutoXdceProcessor()
        {
            _log.Info("CossArbWorker -- Beginning new AutoXdce iteration.");

            try
            {
                var cossAgentConfig = _configClient.GetCossAgentConfig();
                if (!(cossAgentConfig?.IsCossAutoTradingEnabled ?? false))
                {
                    _log.Info("CossArbWorker -- Coss Auto Trading is disabled. Sleeping...");
                }
                else
                {
                    try
                    {
                        _cossArbUtil.AcquireXdceTusd();
                    }
                    catch (Exception exception)
                    {
                        _log.Error(exception);
                    }
                }
            }
            finally
            {
                _log.Info("CossArbWorker -- AutoXdce iteration complete. Sleeping...");
                Thread.Sleep(TimeSpan.FromMinutes(2.5));
            }
        }

        private void AutoTusdProcess()
        {
            _log.Info("CossArbWorker -- Beginning new AutoTusd iteration.");

            try
            {
                if (!IsCossAutoTradingEnabled())
                {
                    _log.Info("CossArbWorker.AutoTusdProcess() -- Coss Auto Trading is disabled. Sleeping...");
                }
                else if (IsBinanceIsOnMaintenance())
                {
                    _log.Info("CossArbWorker.AutoTusdProcess() -- Binance is on maintenance. Sleeping...");
                }
                else
                {
                    var binanceSymbols = new List<string> { "ETH", "BTC" };

                    foreach (var binanceSymbol in binanceSymbols)
                    {
                        try
                        {
                            // Console.WriteLine($"Beginning coss auto eth tusd.");
                            _cossArbUtil.AutoTusdWithReverseBinanceSymbol(binanceSymbol);
                            // Console.WriteLine($"Compeleted coss auto eth tusd.");
                        }
                        catch (Exception exception)
                        {
                            _log.Error($"AutoTusd failed for ETH.");
                            _log.Error(exception);
                        }
                    }
                }
            }
            finally
            {
                _log.Info("CossArbWorker -- AutoTusd iteration complete. Sleeping...");
                Thread.Sleep(TimeSpan.FromMinutes(2.5));
            }
        }

        private void AutoBwtProcess()
        {
            _log.Info("CossArbWorker -- Beginning new AutoBwt iteration.");

            try
            {
                if (!IsCossAutoTradingEnabled())
                {
                    _log.Info("CossArbWorker.AutoBwtProcess() -- Coss Auto Trading is disabled. Sleeping...");
                }
                else if (IsBinanceIsOnMaintenance())
                {
                    _log.Info("CossArbWorker.AutoBwtProcess() -- Binance is on maintenance. Sleeping...");
                }
                else
                {
                    var subJobs = new List<Action>
                    {
                        _cossArbUtil.AcquireXdce,
                        _cossArbUtil.AcquireBwtTusd,
                        _cossArbUtil.AcquireBwtGusd
                    };

                    foreach(var subJob in subJobs)
                    {
                        try
                        {
                            subJob();
                        }
                        catch (Exception exception)
                        {
                            _log.Error(exception);
                        }
                    }
                }
            }
            finally
            {
                _log.Info("CossArbWorker -- AutoTusd iteration complete. Sleeping...");
                Thread.Sleep(TimeSpan.FromMinutes(2.5));
            }
        }

        public void AcquireCossProcessor()
        {
            try
            {                
                if (!IsCossAutoTradingEnabled())
                {
                    _log.Info("CossArbWorker.AcquireCossProcessor() -- Coss Auto Trading is disabled. Sleeping...");
                }
                else if (IsBinanceIsOnMaintenance())
                {
                    _log.Info("CossArbWorker.AcquireCossProcessor() -- Binance is on maintenance. Sleeping...");
                }
                else
                {
                    try { _cossArbUtil.AcquireCossV4(); }
                    catch (Exception exception) { _log.Error(exception); }

                    var cossSymbols = new List<string>() { "LTC", "XEM", "ARK", "DASH", "LSK", "OMG", "ZEN", "NEO", "WAVES"};
                    foreach (var symbol in cossSymbols)
                    {
                        try { _cossArbUtil.AcquireAgainstBinanceSymbolV5(symbol); }
                        catch (Exception exception) { _log.Error(exception); }
                    }
                }
            }
            finally
            {
                _log.Info("CossArbWorker -- AcquireCossProcessor iteration complete. Sleeping...");
                Thread.Sleep(TimeSpan.FromMinutes(2.5));
            }
        }

        //public void AcquireLtcProcessor()
        //{
        //    try
        //    {
        //        if (!IsCossAutoTradingEnabled())
        //        {
        //            _log.Info("CossArbWorker.AcquireLtcProcessor() -- Coss Auto Trading is disabled. Sleeping...");
        //        }
        //        else if (IsBinanceIsOnMaintenance())
        //        {
        //            _log.Info("CossArbWorker.AcquireLtcProcessor() -- Binance is on maintenance. Sleeping...");
        //        }
        //        else
        //        {
        //            _cossArbUtil.AcquireLtc();
        //        }
        //    }
        //    finally
        //    {
        //        var timeToSleep = TimeSpan.FromMinutes(2.5);
        //        _log.Info($"CossArbWorker -- AcquireLtcProcessor iteration complete. Sleeping for {timeToSleep}...");
        //        Thread.Sleep(timeToSleep);
        //    }
        //}

        public void AcquireBchProcessor()
        {
            try
            {
                if (!IsCossAutoTradingEnabled())
                {
                    _log.Info("CossArbWorker.AcquireBchProcessor() -- Coss Auto Trading is disabled. Sleeping...");
                }
                else if (IsBinanceIsOnMaintenance())
                {
                    _log.Info("CossArbWorker.AcquireBchProcessor() -- Binance is on maintenance. Sleeping...");
                }
                else
                {
                    _cossArbUtil.AcquireBchabc();
                    // _arbWorkflowUtil.AutoSymbol("BCHABC", IntegrationNameRes.Coss, IntegrationNameRes.Binance, null, false, true);
                }
            }
            finally
            {
                var timeToSleep = TimeSpan.FromMinutes(2.5);
                _log.Info($"CossArbWorker -- AcquireBchProcessor iteration complete. Sleeping for {timeToSleep}...");
                Thread.Sleep(timeToSleep);
            }
        }

        private void Forever(Action method)
        {
            while (true)
            {
                try
                {
                    method();
                }
                catch (Exception exception)
                {
                    _log.Error(exception);
                }

                Thread.Sleep(100);
            }
        }

        private bool IsBinanceIsOnMaintenance()
        {
            var binanceTradingPairs = _exchangeClient.GetTradingPairs(IntegrationNameRes.Binance, CachePolicy.AllowCache);
            return binanceTradingPairs == null || !binanceTradingPairs.Any();
        }

        private bool IsCossAutoTradingEnabled()
        {
            return _configClient.GetCossAgentConfig()?.IsCossAutoTradingEnabled ?? false;
        }

        private void PerformTimeSync()
        {
            try
            {
                var info = new ProcessStartInfo("w32tm", "/resync");
                info.RedirectStandardOutput = true;
                info.UseShellExecute = false;
                var process = Process.Start(info);
                
                process.WaitForExit();
            }
            catch (Exception exception)
            {
                _log.Error("Windows time sync failed.");
                _log.Error(exception);
            }
        }
    }
}
