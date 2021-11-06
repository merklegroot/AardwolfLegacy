using coss_browser_service_lib.Handlers;
using log_lib;
using rabbit_lib;
using service_lib;
using service_lib.Handlers;
using System.Collections.Generic;
using trade_constants;

namespace coss_browser_service_lib.App
{
    public interface ICossBrowserServiceApp : IServiceApp { }

    public class CossBrowserServiceApp : ServiceApp, ICossBrowserServiceApp
    {
        private readonly ICossBrowserHandler _cossBrowserHandler;

        public CossBrowserServiceApp(
            ICossBrowserHandler cossBrowserHandler,
            IRabbitConnectionFactory rabbitConnectionFactory, 
            ILogRepo log)
            : base(rabbitConnectionFactory, log)
        {
            _cossBrowserHandler = cossBrowserHandler;
        }

        public override string ApplicationName => "Coss Browser Service";

        protected override List<IHandler> Handlers => new List<IHandler>
        {
            _cossBrowserHandler
        };

        protected override string BaseQueueName => TradeRabbitConstants.Queues.CossBrowserServiceQueue;

        protected override int MaxQueueVersion => 1;
    }
}
