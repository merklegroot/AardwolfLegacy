using cryptocompare_service_lib.Handlers;
using log_lib;
using rabbit_lib;
using service_lib;
using service_lib.Handlers;
using System.Collections.Generic;
using trade_constants;

namespace cryptocompare_service_lib.App
{
    public interface ICryptoCompareServiceApp : IServiceApp { }

    public class CryptoCompareServiceApp : ServiceApp, ICryptoCompareServiceApp
    {
        private readonly ICryptoCompareHandler _cryptoCompareHandler;

        public override string ApplicationName => "CryptoCompare Service";
        protected override string BaseQueueName => TradeRabbitConstants.Queues.CryptoCompareServiceQueue;
        protected override int MaxQueueVersion => 1;

        public CryptoCompareServiceApp(
            IRabbitConnectionFactory rabbitConnectionFactory,
            ICryptoCompareHandler valuationHandler,
            ILogRepo log)
            : base(rabbitConnectionFactory, log)
        {
            _cryptoCompareHandler = valuationHandler;
        }

        protected override List<IHandler> Handlers => new List<IHandler>
        {
            _cryptoCompareHandler
        };
    }
}
