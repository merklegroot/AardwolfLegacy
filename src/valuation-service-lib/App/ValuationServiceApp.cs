using log_lib;
using rabbit_lib;
using service_lib;
using service_lib.Handlers;
using System.Collections.Generic;
using trade_constants;
using valuation_service_lib.Handlers;

namespace valuation_service_lib.App
{
    public interface IValuationServiceApp : IServiceApp { }

    public class ValuationServiceApp : ServiceApp, IValuationServiceApp
    {
        private readonly IValuationHandler _valuationHandler;

        public override string ApplicationName => "Valuation Service";

        public ValuationServiceApp(
            IRabbitConnectionFactory rabbitConnectionFactory,
            IValuationHandler valuationHandler,
            ILogRepo log)
            : base(rabbitConnectionFactory, log)
        {
            _valuationHandler = valuationHandler;
        }

        protected override List<IHandler> Handlers => new List<IHandler>
        {
            _valuationHandler
        };

        protected override string BaseQueueName => TradeRabbitConstants.Queues.ValuationServiceQueue;

        protected override int MaxQueueVersion => 1;
    }
}
