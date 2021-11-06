using browser_automation_service_lib.App;
using service_lib;
using StructureMap;
using System;

namespace browser_automation_service_con
{
    public interface IBrowserAutomationServiceRunner : IServiceRunner
    { }

    public class BrowserAutomationServiceRunner : IBrowserAutomationServiceRunner
    {
        public event Action OnStarted;

        public void Run(string overriddenQueueName = null)
        {
            var container = Container.For<BrowserAutomationServiceRegistry>();
            var service = container.GetInstance<IBrowserAutomationServiceApp>();

            if (!string.IsNullOrWhiteSpace(overriddenQueueName))
            {
                service.OverrideQueue(overriddenQueueName);
            }

            service.OnStarted += () => { OnStarted?.Invoke(); };
            try
            {
                service.Run();
            }
            finally
            {
                service.Dispose();
            }
        }
    }
}
