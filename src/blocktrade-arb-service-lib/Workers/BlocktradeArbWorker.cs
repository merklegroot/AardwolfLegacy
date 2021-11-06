using arb_service_lib;
using arb_workflow_lib;
using log_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using trade_constants;

namespace blocktrade_arb_service_lib.Workers
{
    public interface IBlocktradeArbWorker : IArbWorker { }

    public class BlocktradeArbWorker : ArbWorker, IBlocktradeArbWorker
    {
        private readonly IArbWorkflowUtil _arbWorkflowUtil;
        private readonly ILogRepo _log;

        public BlocktradeArbWorker(
            IArbWorkflowUtil arbWorkflowUtil,
            ILogRepo log) : base(log)
        {
            _arbWorkflowUtil = arbWorkflowUtil;
            _log = log;
        }

        protected override List<Action> Jobs => new List<Action>
        {
            SeriesJob
        };

        private void SeriesJob()
        {
            var items = new List<Action>()
            {
                ProcessEthBtc,
                ProcessSymbols
            };

            ExecuteSerial(items);

            Thread.Sleep(TimeSpan.FromSeconds(30));
        }

        private void ProcessSymbols()
        {
            var items = new List<Action>
            {
                () =>
                {
                     var hitbtcChxOpenBidQuantityDictionary = new Dictionary<string, decimal> { { "ETH", 1.0m } };

                    _arbWorkflowUtil.AutoSymbol(
                        "CHX", IntegrationNameRes.Blocktrade, IntegrationNameRes.Idex, null, true, true, 100
                        , null, hitbtcChxOpenBidQuantityDictionary);
                },

                () => AutoHitBtc(),
                () => AutoQryptos(),

                () => _arbWorkflowUtil.AutoStraddle(IntegrationNameRes.Blocktrade, "BTT", "ETH"),

                () => _arbWorkflowUtil.AutoSymbol("BAT", IntegrationNameRes.Blocktrade, IntegrationNameRes.Binance, null, true, false, 100),
                () => _arbWorkflowUtil.AutoSymbol("LTC", IntegrationNameRes.Blocktrade, IntegrationNameRes.Binance, null, true, false, 100),
                () => _arbWorkflowUtil.AutoSymbol("XRP", IntegrationNameRes.Blocktrade, IntegrationNameRes.Binance, null, true, false, 100),
                () => _arbWorkflowUtil.AutoSymbol("BCH", IntegrationNameRes.Blocktrade, IntegrationNameRes.Binance, null, true, false, 100),
                () => _arbWorkflowUtil.AutoSymbol("XLM", IntegrationNameRes.Blocktrade, IntegrationNameRes.Binance, null, true, false, 100),

                // () => _arbWorkflowUtil.AutoSymbol("KAYA", IntegrationNameRes.Blocktrade, IntegrationNameRes.Coss, null, true, false, 100, 35, new Dictionary<string, decimal> { { "ETH", 0.15m } })
            };

            ExecuteSerial(items, TimeSpan.FromSeconds(1.5));
        }

        private void AutoQryptos()
        {
            var qryptosJobs = new List<Action>
            {
                () => _arbWorkflowUtil.AutoSymbol("MITH", IntegrationNameRes.Qryptos, IntegrationNameRes.Binance, null, true),
                () => _arbWorkflowUtil.AutoSymbol("OAX", IntegrationNameRes.Qryptos, IntegrationNameRes.Binance, null, true),
                () => _arbWorkflowUtil.AutoSymbol("MCO", IntegrationNameRes.Qryptos, IntegrationNameRes.Binance, null, true)
            };

            ExecuteSerial(qryptosJobs, TimeSpan.FromSeconds(1.5));
        }

        private void AutoHitBtc()
        {
            const decimal MaxHitBtcChxValueToOwn = 1000.0m;
            const decimal HitBtcIdealPercentDiff = 5.0m;

            var hitBtcJobs = new List<Action>
            {
                () => _arbWorkflowUtil.AutoSymbol("CHX", IntegrationNameRes.HitBtc, IntegrationNameRes.Idex, null, true, true, MaxHitBtcChxValueToOwn, HitBtcIdealPercentDiff)
            };

            ExecuteSerial(hitBtcJobs, TimeSpan.FromSeconds(1.5));
        }

        private void ProcessEthBtc()
        {
            _arbWorkflowUtil.AutoEthBtc(IntegrationNameRes.Blocktrade);
        }

        private void ExecuteSerial(List<Action> actions, TimeSpan? sleepBetween = null)
        {
            if (actions == null || !actions.Any())
            {
                foreach (var action in actions)
                {
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception exception)
                    {
                        _log.Error(exception);
                    }

                    if (sleepBetween.HasValue && sleepBetween > TimeSpan.Zero)
                    {
                        Thread.Sleep(sleepBetween.Value);
                    }
                }
            }
        }
    }
}
