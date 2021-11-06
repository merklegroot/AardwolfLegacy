using System.Collections.Generic;
using coss_arb_service_lib.Handlers;
using coss_arb_service_lib.Workflows;
using log_lib;
using rabbit_lib;
using service_lib;
using service_lib.Handlers;
using task_lib;
using trade_constants;

namespace coss_arb_service_lib.App
{
    public interface ICossArbServiceApp : IServiceApp
    {
        void RunBackgroundProcess();
    }

    public class CossArbServiceApp : ServiceApp, ICossArbServiceApp
    {
        private readonly ICossArbHandler _cossArbHandler;
        private readonly ICossArbWorker _cossArbWorkflow;

        public CossArbServiceApp(
            ICossArbHandler cossArbHandler,
            ICossArbWorker cossArbWorkflow,
            IRabbitConnectionFactory rabbitConnectionFactory,
            ILogRepo log)
            : base(rabbitConnectionFactory, log)
        {
            _cossArbHandler = cossArbHandler;
            _cossArbWorkflow = cossArbWorkflow;
        }

        public override string ApplicationName => "Coss Arb Service";

        protected override List<IHandler> Handlers => new List<IHandler> { _cossArbHandler };

        protected override string BaseQueueName => TradeRabbitConstants.Queues.CossArbServiceQueue;

        protected override int MaxQueueVersion => 1;

        public void RunBackgroundProcess()
        {
            LongRunningTask.Run(() => _cossArbWorkflow.Run());
        }
    }
}
