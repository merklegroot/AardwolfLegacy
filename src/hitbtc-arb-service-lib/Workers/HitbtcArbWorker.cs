using arb_service_lib;
using arb_workflow_lib;
using hitbtc_arb_service_lib.Workflow;
using log_lib;
using System;
using System.Collections.Generic;
using System.Threading;

namespace hitbtc_arb_service_lib.Workers
{
    public interface IHitbtcArbWorker : IArbWorker { }

    public class HitbtcArbWorker : ArbWorker, IHitbtcArbWorker
    {
        private readonly IArbWorkflowUtil _arbWorkflowUtil;
        private readonly IHitbtcArbWorkflowUtil _hitbtcArbWorkflowUtil;
        private readonly ILogRepo _log;

        public HitbtcArbWorker(
            IArbWorkflowUtil arbWorkflowUtil,
            IHitbtcArbWorkflowUtil hitbtcArbWorkflowUtil,
            ILogRepo log)
            : base(log)
        {
            _arbWorkflowUtil = arbWorkflowUtil;
            _hitbtcArbWorkflowUtil = hitbtcArbWorkflowUtil;
            _log = log;
        }

        protected override List<Action> Jobs => new List<Action> { AutoHitbtcCoss };

        private void AutoHitbtcCoss()
        {
            Console.WriteLine("Beginning Auto Hitbtc-Coss process.");

            try
            {
                _hitbtcArbWorkflowUtil.AutoHitbtcCoss();
            }
            catch (Exception exception)
            {
                _log.Error($"An exception occurred in the AutoHitbtcCoss process.{Environment.NewLine}{exception.Message}");
                _log.Error(exception);
            }
            finally
            {
                Console.WriteLine("Completed Auto Hitbtc-Coss process.");
            }
            
            Thread.Sleep(TimeSpan.FromSeconds(30));
        }
    }
}
