using System.Collections.Generic;
using log_lib;
using rabbit_lib;
using service_lib;
using service_lib.Handlers;

namespace arb_service_lib
{
    public interface IArbServiceApp : IServiceApp
    {
        void RunBackgroundProcess();
    }

    public abstract class ArbServiceApp : ServiceApp, IArbServiceApp
    {
        private readonly IArbWorker _arbWorker;

        public ArbServiceApp(
            IRabbitConnectionFactory rabbitConnectionFactory,
            IArbWorker arbWorker,
            ILogRepo log)
            : base(rabbitConnectionFactory, log)
        {
            _arbWorker = arbWorker;
        }

        public virtual void RunBackgroundProcess()
        {
            _arbWorker.Run();
        }

        protected override List<IHandler> Handlers => new List<IHandler> { };

        protected override int MaxQueueVersion => 1;
    }
}
