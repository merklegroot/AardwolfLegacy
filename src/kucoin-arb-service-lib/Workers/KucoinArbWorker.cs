using arb_workflow_lib;
using log_lib;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using task_lib;
using trade_constants;

namespace kucoin_arb_service_lib.Workers
{
    public interface IKucoinArbWorker
    {
        void Run();
    }

    public class KucoinArbWorker : IKucoinArbWorker
    {
        private readonly IArbWorkflowUtil _arbWorkflowUtil;
        private readonly ILogRepo _log;

        public KucoinArbWorker(
            IArbWorkflowUtil arbWorkflowUtil,
            ILogRepo log)
        {
            _arbWorkflowUtil = arbWorkflowUtil;
            _log = log;
        }

        public void Run()
        {
            var jobs = new List<Action>
            {
                // AutoSymbolProcess,
                // AutoUsdc
                // StraddleCan,
                AutoSell,
                // AutoStraddle
                AutoBuy
            };

            var tasks = new List<Task>();
            foreach (var job in jobs)
            {
                tasks.Add(LongRunningTask.Run(() => Forever(job)));
            }

            tasks.ForEach(task => task.Wait());
        }

        private void AutoUsdc()
        {
            const string JobDesc = "Auto-USDC";

            _log.Info($"Beginning the {JobDesc} job.");
            try
            {
                _arbWorkflowUtil.KucoinUsdc();
            }
            catch (Exception exception)
            {
                _log.Error($"An error occurred in the {JobDesc} job.");
                _log.Error(exception);
            }

            var timeToSleep = TimeSpan.FromSeconds(20);
            _log.Info($"The {JobDesc} job has completed. Will execute again in {timeToSleep.TotalSeconds} seconds.");

            Thread.Sleep(timeToSleep);
        }

        private void StraddleCan()
        {
            _arbWorkflowUtil.AutoStraddle(IntegrationNameRes.KuCoin, "CAN", "ETH");
            _arbWorkflowUtil.AutoStraddle(IntegrationNameRes.KuCoin, "CAN", "BTC");
            _arbWorkflowUtil.AutoStraddle(IntegrationNameRes.KuCoin, "VNX", "ETH");

            // _arbWorkflowUtil.AutoSell(IntegrationNameRes.KuCoin, "LA", "ETH");
        }

        private void AutoSell()
        {
            // _arbWorkflowUtil.AutoSell(IntegrationNameRes.KuCoin, "LA", "ETH");
            _arbWorkflowUtil.AutoSell(IntegrationNameRes.KuCoin, "LALA", "ETH");
            // _arbWorkflowUtil.AutoSell(IntegrationNameRes.KuCoin, "DAT", "ETH");
        }

        private void AutoBuy()
        {
            _arbWorkflowUtil.AcquireUsdcEth();
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

                Thread.Sleep(10);
            }
        }
    }
}
