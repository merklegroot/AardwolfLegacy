using System.Collections.Generic;
using log_lib;
using bitz_arb_service_lib.Handlers;
using bitz_arb_service_lib.Workers;
using rabbit_lib;
using service_lib;
using service_lib.Handlers;
using task_lib;
using trade_constants;

namespace bitz_arb_service_lib.App
{
    public interface IBitzArbServiceApp : IServiceApp
    {
        void RunBackgroundProcess();
    }

    public class BitzArbServiceApp : ServiceApp, IBitzArbServiceApp
    {
        private readonly IBitzArbHandler _bitzArbHandler;
        private readonly IBitzArbWorker _bitzArbWorker;

        public BitzArbServiceApp(
            IBitzArbHandler bitzArbHandler,
            IBitzArbWorker bitzArbWorker,
            IRabbitConnectionFactory rabbitConnectionFactory,
            ILogRepo log)
            : base(rabbitConnectionFactory, log)
        {
            _bitzArbHandler = bitzArbHandler;
            _bitzArbWorker = bitzArbWorker;
        }

        public override string ApplicationName => "Bitz Arb Service";

        protected override List<IHandler> Handlers => new List<IHandler>
        {
            _bitzArbHandler
        };

        protected override string BaseQueueName => TradeRabbitConstants.Queues.BitzArbServiceQueue;

        protected override int MaxQueueVersion => 1;

        public void RunBackgroundProcess()
        {
            LongRunningTask.Run(() => _bitzArbWorker.Run());
        }
    }
}
