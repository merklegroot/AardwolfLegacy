using System.Collections.Generic;
using exchange_service_lib.Constants;
using exchange_service_lib.Handlers;
using log_lib;
using rabbit_lib;
using service_lib;
using service_lib.Handlers;
using trade_constants;

namespace exchange_service_lib.App
{
    public interface IExchangeServiceApp : IServiceApp { }
    public class ExchangeServiceApp : ServiceApp, IExchangeServiceApp
    {
        private IExchangeHandler _exchangeHandler;

        public ExchangeServiceApp(
            IRabbitConnectionFactory rabbitConnectionFactory,
            IExchangeHandler exchangeHandler,
            ILogRepo log)
            : base(rabbitConnectionFactory, log)
        {
            _exchangeHandler = exchangeHandler;
        }

        public override string ApplicationName => "Exchange Service";

        protected override List<IHandler> Handlers => new List<IHandler>
        {
            _exchangeHandler
        };

        protected override string BaseQueueName => TradeRabbitConstants.Queues.ExchangeServiceQueue;

        protected override int MaxQueueVersion => ExchangeServiceConstants.Version;
    }
}
