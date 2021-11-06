using browser_automation_service_lib.Handlers;
using log_lib;
using rabbit_lib;
using service_lib;
using service_lib.Handlers;
using System.Collections.Generic;
using trade_constants;

namespace browser_automation_service_lib.App
{
    public interface IBrowserAutomationServiceApp : IServiceApp { }

    public class BrowserAutomationServiceApp : ServiceApp, IBrowserAutomationServiceApp
    {
        private readonly IBrowserAutomationHandler _handler;

        public override string ApplicationName => "Browser Automation Service";

        public BrowserAutomationServiceApp(
            IBrowserAutomationHandler handler,
            IRabbitConnectionFactory rabbitConnectionFactory,
            ILogRepo log)
            : base(rabbitConnectionFactory, log)
        {
            _handler = handler;
        }

        protected override List<IHandler> Handlers => new List<IHandler> { _handler };

        protected override string BaseQueueName => TradeRabbitConstants.Queues.BrowserAutomationServiceQueue;

        protected override int MaxQueueVersion => 1;
    }
}