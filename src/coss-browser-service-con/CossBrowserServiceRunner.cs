using coss_browser_service_lib.App;
using coss_browser_service_lib.Workers;
using service_lib;
using StructureMap;
using System;
using task_lib;

namespace coss_browser_service_con
{
    public interface ICossBrowserServiceRunner : IServiceRunner
    { }

    public class CossBrowserServiceRunner : ICossBrowserServiceRunner
    {
        public event Action OnStarted;

        public void Run(string overriddenQueueName = null)
        {
            var container = Container.For<CossBrowserServiceRegistry>();

            var chromeWorker = container.GetInstance<IChromeProcessWorker>();
            var chromeWorkerTask = LongRunningTask.Run(() =>
            {
                chromeWorker.Start();
            });

            var service = container.GetInstance<ICossBrowserServiceApp>();

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
                chromeWorker.Stop();
            }
        }
    }
}
