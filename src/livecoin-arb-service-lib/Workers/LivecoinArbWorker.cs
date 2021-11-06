using arb_service_lib;
using arb_workflow_lib;
using cache_lib.Models;
using exchange_client_lib;
using log_lib;
using System;
using System.Collections.Generic;
using System.Threading;
using trade_constants;

namespace livecoin_arb_service_lib.Workers
{
    public interface ILivecoinArbWorker : IArbWorker { }

    public class LivecoinArbWorker : ArbWorker, ILivecoinArbWorker
    {
        private readonly IArbWorkflowUtil _arbWorkflowUtil;
        private readonly IExchangeClient _exchangeClient;
        private readonly ILogRepo _log;

        public LivecoinArbWorker(
            IArbWorkflowUtil arbWorkflowUtil,
            IExchangeClient exchangeClient,
            ILogRepo log) : base(log)
        {
            _arbWorkflowUtil = arbWorkflowUtil;
            _exchangeClient = exchangeClient;
            _log = log;
        }

        protected override List<Action> Jobs => new List<Action>
        {
            ProcessRep,
            UpdateHistory
        };

        private void UpdateHistory()
        {
            var response = _exchangeClient.GetExchangeHistory(IntegrationNameRes.Livecoin, 0, CachePolicy.AllowCache);
            Thread.Sleep(TimeSpan.FromHours(4));
        }

        private void ProcessRep()
        {
            const string Symbol = "REP";
            _log.Info($"Beginning livecoin autosymbol for {Symbol}.");
            try
            {
                _arbWorkflowUtil.AutoSymbol(Symbol, IntegrationNameRes.Livecoin, IntegrationNameRes.Binance);
            }
            catch(Exception exception)
            {
                _log.Error(exception);
            }
            finally
            {
                var sleepTime = TimeSpan.FromSeconds(30);

                _log.Info($"Done with livecoin autosymbol for {Symbol}. Sleeping for {sleepTime.TotalSeconds} seconds");                
                Thread.Sleep(sleepTime);
            }            
        }
    }
}
