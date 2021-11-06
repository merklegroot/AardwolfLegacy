using System.Collections.Generic;
using log_lib;
using qryptos_arb_service_lib.Handlers;
using qryptos_arb_service_lib.Workers;
using rabbit_lib;
using service_lib;
using service_lib.Handlers;
using task_lib;
using trade_constants;

namespace qryptos_arb_service_lib.App
{
    public interface IQryptosArbServiceApp : IServiceApp
    {
        void RunBackgroundProcess();
    }

    public class QryptosArbServiceApp : ServiceApp, IQryptosArbServiceApp
    {
        private readonly IQryptosArbHandler _qryptosArbHandler;
        private readonly IQryptosArbWorker _qryptosArbWorker;

        public QryptosArbServiceApp(
            IQryptosArbHandler qryptosArbHandler,
            IQryptosArbWorker qryptosArbWorker,
            IRabbitConnectionFactory rabbitConnectionFactory, 
            ILogRepo log)
            : base(rabbitConnectionFactory, log)
        {
            _qryptosArbHandler = qryptosArbHandler;
            _qryptosArbWorker = qryptosArbWorker;
        }

        public override string ApplicationName => "Qryptos Arb Service";

        protected override List<IHandler> Handlers => new List<IHandler>
        {
            _qryptosArbHandler
        };

        protected override string BaseQueueName => TradeRabbitConstants.Queues.QryptosArbServiceQueue;

        protected override int MaxQueueVersion => 1;

        public void RunBackgroundProcess()
        {
            LongRunningTask.Run(() => _qryptosArbWorker.Run());
        }
    }
}
