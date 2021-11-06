using System.Collections.Generic;
using log_lib;
using kucoin_arb_service_lib.Handlers;
using kucoin_arb_service_lib.Workers;
using rabbit_lib;
using service_lib;
using service_lib.Handlers;
using task_lib;
using trade_constants;

namespace kucoin_arb_service_lib.App
{
    public interface IKucoinArbServiceApp : IServiceApp
    {
        void RunBackgroundProcess();
    }

    public class KucoinArbServiceApp : ServiceApp, IKucoinArbServiceApp
    {
        private readonly IKucoinArbHandler _kucoinArbHandler;
        private readonly IKucoinArbWorker _kucoinArbWorker;

        public KucoinArbServiceApp(
            IKucoinArbHandler kucoinArbHandler,
            IKucoinArbWorker kucoinArbWorker,
            IRabbitConnectionFactory rabbitConnectionFactory,
            ILogRepo log)
            : base(rabbitConnectionFactory, log)
        {
            _kucoinArbHandler = kucoinArbHandler;
            _kucoinArbWorker = kucoinArbWorker;
        }

        public override string ApplicationName => "Kucoin Arb Service";

        protected override List<IHandler> Handlers => new List<IHandler>
        {
            _kucoinArbHandler
        };

        protected override string BaseQueueName => TradeRabbitConstants.Queues.KucoinArbServiceQueue;

        protected override int MaxQueueVersion => 1;

        public void RunBackgroundProcess()
        {
            LongRunningTask.Run(() => _kucoinArbWorker.Run());
        }
    }
}
