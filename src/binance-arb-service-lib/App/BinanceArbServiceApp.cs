using arb_service_lib;
using binance_arb_service_lib.Workers;
using log_lib;
using rabbit_lib;
using trade_constants;

namespace binance_arb_service_lib.App
{
    public interface IBinanceArbServiceApp : IArbServiceApp { }

    public class BinanceArbServiceApp : ArbServiceApp, IBinanceArbServiceApp
    {
        public BinanceArbServiceApp(
            IRabbitConnectionFactory rabbitConnectionFactory,
            IBinanceArbWorker worker,
            ILogRepo log)
            : base(rabbitConnectionFactory, worker, log)
        {
        }

        public override string ApplicationName => "binance-arb-service";

        protected override string BaseQueueName => TradeRabbitConstants.Queues.BinanceArbServiceQueue;
    }
}
