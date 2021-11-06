using arb_service_lib;
using livecoin_arb_service_lib.Workers;
using log_lib;
using rabbit_lib;
using service_lib;
using trade_constants;

namespace livecoin_arb_service_lib.App
{
    public interface ILivecoinArbServiceApp : IServiceApp
    { }

    public class LivecoinArbServiceApp : ArbServiceApp, ILivecoinArbServiceApp
    {
        public LivecoinArbServiceApp(
            IRabbitConnectionFactory rabbitConnectionFactory,
            ILivecoinArbWorker worker,
            ILogRepo log)
            : base(rabbitConnectionFactory, worker, log)
        {
        }

        public override string ApplicationName => "Livecoin Arb Service";
        
        protected override string BaseQueueName => TradeRabbitConstants.Queues.LivecoinArbServiceQueue;
    }
}
