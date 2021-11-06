using arb_workflow_lib;
using log_lib;
using Newtonsoft.Json;
using bitz_arb_service_lib.Models;
using bitz_arb_service_lib.res;
using res_util_lib;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using task_lib;
using trade_constants;

namespace bitz_arb_service_lib.Workers
{
    public interface IBitzArbWorker
    {
        void Run();
    }

    public class BitzArbWorker : IBitzArbWorker
    {
        private readonly IArbWorkflowUtil _arbWorkflowUtil;
        private readonly ILogRepo _log;

        public BitzArbWorker(
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
                AutoSymbolProcess,
            };

            var tasks = new List<Task>();
            foreach (var job in jobs)
            {
                tasks.Add(LongRunningTask.Run(() => Forever(job)));
            }

            tasks.ForEach(task => task.Wait());
        }

        private void AutoSymbolProcess()
        {
            // AutoTusd();

            try
            {
                var defs = ResUtil.Get<List<AutoSymbolDef>>("arb-defs.json", typeof(BitzArbServiceLibResDummy).Assembly);

                foreach (var def in defs)
                {
                    var serializedDef = JsonConvert.SerializeObject(def);

                    _log.Info($"Beginning auto symbol for {serializedDef}.");

                    try
                    {
                        _arbWorkflowUtil.AutoSymbol(def.Symbol, def.ArbExchange, def.CompExchange, def.AltBaseSymbol);
                    }
                    catch (Exception exception)
                    {
                        _log.Error($"BitzArbWorker - Auto Symbol failed for def:{Environment.NewLine}{serializedDef}{Environment.NewLine}{exception.Message}.");
                        _log.Error(exception);
                    }
                }
            }
            catch (Exception exception)
            {
                _log.Error(exception);
            }
        }

        private void AutoTusd()
        {
            try
            {
                _arbWorkflowUtil.AutoReverseXusd(IntegrationNameRes.Bitz, "TUSD", "BTC");
            }
            catch (Exception exception)
            {
                _log.Error(exception);
            }

            try
            {
                _arbWorkflowUtil.AutoReverseXusd(IntegrationNameRes.Bitz, "TUSD", "ETH");
            }
            catch (Exception exception)
            {
                _log.Error(exception);
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

                Thread.Sleep(10);
            }
        }
    }
}
