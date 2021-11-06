using arb_service_lib;
using hitbtc_arb_service_lib.Workers;
using log_lib;
using rabbit_lib;
using trade_constants;

namespace hitbtc_arb_service_lib.App
{
    public interface IHitBtcArbServiceApp : IArbServiceApp { }

    public class HitbtcArbServiceApp : ArbServiceApp, IHitBtcArbServiceApp
    {
        public HitbtcArbServiceApp(
            IRabbitConnectionFactory rabbitConnectionFactory,
            IHitbtcArbWorker hitbtcArbWorker,
            ILogRepo log)
            : base(rabbitConnectionFactory, hitbtcArbWorker, log)
        {
        }

        public override string ApplicationName => "hitbtc-arb-service";

        protected override string BaseQueueName => TradeRabbitConstants.Queues.HitbtcArbServiceQueue;
    }
}
