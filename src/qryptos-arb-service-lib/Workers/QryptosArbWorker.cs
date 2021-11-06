using arb_workflow_lib;
using log_lib;
using Newtonsoft.Json;
using qryptos_arb_service_lib.Models;
using qryptos_arb_service_lib.res;
using res_util_lib;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using task_lib;
using trade_res;

namespace qryptos_arb_service_lib.Workers
{
    public interface IQryptosArbWorker
    {
        void Run();
    }

    public class QryptosArbWorker : IQryptosArbWorker
    {
        private readonly IArbWorkflowUtil _arbWorkflowUtil;
        private readonly ILogRepo _log;

        public QryptosArbWorker(
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
            try
            {
                var defs = ResUtil.Get<List<AutoSymbolDef>>("arb-defs.json", typeof(QryptosArbServiceLibResDummy).Assembly);

                foreach (var def in defs)
                {
                    var serializedDef = JsonConvert.SerializeObject(def);

                    var infoText = $"Beginning auto symbol for {serializedDef}.";
                    Console.WriteLine($"Beginning auto symbol for {serializedDef}.");

                    try
                    {
                        _arbWorkflowUtil.AutoSymbol(def.Symbol, def.ArbExchange, def.CompExchange, def.AltBaseSymbol);
                    }
                    catch (Exception exception)
                    {

                        _log.Error($"QryptosArbWorker - Auto Symbol failed for def:{Environment.NewLine}{serializedDef}{Environment.NewLine}{exception.Message}.");
                        _log.Error(exception);
                    }
                }
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
